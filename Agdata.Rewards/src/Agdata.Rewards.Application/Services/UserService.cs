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
        var emailValueObject = new Email(email);
        var employeeIdValueObject = new EmployeeId(employeeId);

        if (await _userRepository.GetByEmailAsync(emailValueObject) is not null)
        {
            throw new DomainException("A user with this email already exists.");
        }

        if (await _userRepository.GetByEmployeeIdAsync(employeeIdValueObject) is not null)
        {
            throw new DomainException("A user with this employee ID already exists.");
        }

        var newUser = User.CreateNew(name, email, employeeId);

        _userRepository.Add(newUser);
        await _unitOfWork.SaveChangesAsync();

        return newUser;
    }
}