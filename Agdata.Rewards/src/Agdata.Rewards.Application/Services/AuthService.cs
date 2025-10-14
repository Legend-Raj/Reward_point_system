using System;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminRegistry _adminRegistry;

    public AuthService(IUserRepository userRepository, IAdminRegistry adminRegistry)
    {
        _userRepository = userRepository;
        _adminRegistry = adminRegistry;
    }

    public async Task<User> AuthenticateAsync(string email, string employeeId)
    {
        var emailAddress = new Email(email);
        var employeeIdentifier = new EmployeeId(employeeId);

        var user = await _userRepository.GetUserByEmailAsync(emailAddress)
            ?? await _userRepository.GetUserByEmployeeIdAsync(employeeIdentifier)
            ?? throw new DomainException(DomainErrors.User.NotFound);

        if (!user.IsActive)
        {
            throw new DomainException(DomainErrors.User.AccountInactive);
        }

        var emailMatches = string.Equals(user.Email.Value, emailAddress.Value, StringComparison.OrdinalIgnoreCase);
        var employeeMatches = string.Equals(user.EmployeeId.Value, employeeIdentifier.Value, StringComparison.OrdinalIgnoreCase);

        if (!emailMatches && !employeeMatches)
        {
            throw new DomainException(DomainErrors.Authorization.InvalidCredentials);
        }

        return user;
    }

    public bool IsAdmin(string email, string employeeId)
    {
        return _adminRegistry.IsAdmin(email, employeeId);
    }
}