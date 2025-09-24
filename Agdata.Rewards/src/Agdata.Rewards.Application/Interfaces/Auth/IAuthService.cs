using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<User> ProvisionUserAsync(string name, string email, string employeeId);
}