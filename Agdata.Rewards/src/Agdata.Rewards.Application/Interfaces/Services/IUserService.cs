using System;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IUserService
{
    Task<User> CreateNewUserAsync(string name, string email, string employeeId);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmployeeIdAsync(string employeeId);
    Task<User> UpdateUserAsync(Guid id, string? name, string? email, string? employeeId, bool? isActive);
}