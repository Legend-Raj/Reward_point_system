using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<User> AuthenticateAsync(string email, string employeeId);
    bool IsAdmin(string email, string employeeId);
}