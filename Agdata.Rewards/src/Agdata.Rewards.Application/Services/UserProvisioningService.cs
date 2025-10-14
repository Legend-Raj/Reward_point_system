using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Application.Services;

public class UserProvisioningService : IUserProvisioningService
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminRegistry _adminRegistry;
    private readonly IUnitOfWork _unitOfWork;

    public UserProvisioningService(IUserRepository userRepository, IAdminRegistry adminRegistry, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _adminRegistry = adminRegistry;
        _unitOfWork = unitOfWork;
    }

    public async Task<User> ProvisionUserAsync(string firstName, string? middleName, string lastName, string email, string employeeId)
    {
        var emailAddress = new Email(email);
        var employeeIdentifier = new EmployeeId(employeeId);

        var existing = await _userRepository.GetUserByEmailAsync(emailAddress);
        if (existing is not null)
        {
            return existing;
        }

        existing = await _userRepository.GetUserByEmployeeIdAsync(employeeIdentifier);
        if (existing is not null)
        {
            return existing;
        }

        var personName = PersonName.Create(firstName, middleName, lastName);

        User newAccount = _adminRegistry.IsAdmin(email, employeeId)
            ? Admin.CreateNew(personName.FirstName, personName.MiddleName, personName.LastName, email, employeeId)
            : User.CreateNew(personName.FirstName, personName.MiddleName, personName.LastName, email, employeeId);

        _userRepository.AddUser(newAccount);
        await _unitOfWork.SaveChangesAsync();

        return newAccount;
    }
}
