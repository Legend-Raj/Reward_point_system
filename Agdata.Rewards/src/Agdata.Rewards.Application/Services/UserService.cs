using System;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<User> CreateNewUserAsync(string name, string email, string employeeId)
    {
        var emailAddress = new Email(email);
        var employeeIdentifier = new EmployeeId(employeeId);

        if (await _userRepository.GetByEmailAsync(emailAddress) is not null)
        {
            throw new DomainException("A user with this email already exists.");
        }

        if (await _userRepository.GetByEmployeeIdAsync(employeeIdentifier) is not null)
        {
            throw new DomainException("A user with this employee ID already exists.");
        }

        var newUser = User.CreateNew(name, email, employeeId);

        _userRepository.Add(newUser);
        await _unitOfWork.SaveChangesAsync();

        return newUser;
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _userRepository.GetByIdAsync(id);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var emailAddress = new Email(email);
        return _userRepository.GetByEmailAsync(emailAddress);
    }

    public Task<User?> GetByEmployeeIdAsync(string employeeId)
    {
        var employeeIdentifier = new EmployeeId(employeeId);
        return _userRepository.GetByEmployeeIdAsync(employeeIdentifier);
    }

    public async Task<User> UpdateUserAsync(Guid id, string? name, string? email, string? employeeId, bool? isActive)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new DomainException("User not found.");

        if (!string.IsNullOrWhiteSpace(name))
        {
            user.UpdateName(name);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var desiredEmail = new Email(email);
            var existing = await _userRepository.GetByEmailAsync(desiredEmail);
            if (existing is not null && existing.Id != user.Id)
            {
                throw new DomainException("A user with this email already exists.");
            }

            user.UpdateEmail(desiredEmail);
        }

        if (!string.IsNullOrWhiteSpace(employeeId))
        {
            var desiredEmployeeId = new EmployeeId(employeeId);
            var existingEmployee = await _userRepository.GetByEmployeeIdAsync(desiredEmployeeId);
            if (existingEmployee is not null && existingEmployee.Id != user.Id)
            {
                throw new DomainException("A user with this employee ID already exists.");
            }

            user.UpdateEmployeeId(desiredEmployeeId);
        }

        if (isActive.HasValue)
        {
            if (isActive.Value)
            {
                user.ActivateAccount();
            }
            else
            {
                user.DeactivateAccount();
            }
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }
}