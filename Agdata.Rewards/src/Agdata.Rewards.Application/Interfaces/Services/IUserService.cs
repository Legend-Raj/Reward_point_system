using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IUserService
{
    Task<User> CreateNewUserAsync(string name, string email, string employeeId);
}