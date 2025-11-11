using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IUserRepository
{
    /// <summary>Fetches a user by their unique identifier.</summary>
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdForUpdateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmployeeIdAsync(EmployeeId employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetTop3EmployeesWithHighestRewardsAsync(CancellationToken cancellationToken = default);
    void AddUser(User user);
    void UpdateUser(User user);
}