using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IUserProvisioningService
{
    Task<User> ProvisionUserAsync(string firstName, string? middleName, string lastName, string email, string employeeId);
}
