using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminRegistry _adminRegistry;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUserRepository userRepository, IAdminRegistry adminRegistry, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _adminRegistry = adminRegistry;
        _unitOfWork = unitOfWork;
    }

    public async Task<User> ProvisionUserAsync(string name, string email, string employeeId)
    {
        var emailValueObject = new Email(email);
        var existingUser = await _userRepository.GetByEmailAsync(emailValueObject);

        if (existingUser is not null)
        {
            return existingUser;
        }

        User newAccount = _adminRegistry.IsAdmin(email, employeeId)
            ? Admin.CreateNew(name, email, employeeId)
            : User.CreateNew(name, email, employeeId);

        _userRepository.Add(newAccount);
        await _unitOfWork.SaveChangesAsync();

        return newAccount;
    }
}