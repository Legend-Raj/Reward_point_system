using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IUserRepository
{
    /// <summary>Fetches a user by their unique identifier.</summary>
    Task<User?> GetUserByIdAsync(Guid userId);

    /// <summary>Finds a user by their unique email address.</summary>
    Task<User?> GetUserByEmailAsync(Email email);

    /// <summary>Finds a user by their employee identifier.</summary>
    Task<User?> GetUserByEmployeeIdAsync(EmployeeId employeeId);

    /// <summary>Returns all users stored in the repository.</summary>
    Task<IReadOnlyList<User>> ListUsersAsync();

    /// <summary>Persists a newly created user to the repository.</summary>
    void AddUser(User user);

    /// <summary>Persists updates to an existing user.</summary>
    void UpdateUser(User user);
}