using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agdata.Rewards.Application.DTOs.Users;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Application.DTOs.Common;

namespace Agdata.Rewards.Application.Interfaces.Services;

/// <summary>
/// Provides user lifecycle operations including creation, querying, listing, and updates.
/// </summary>
public interface IUserService
{
    Task<User> CreateNewUserAsync(string firstName, string? middleName, string lastName, string email, string employeeId);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmployeeIdAsync(string employeeId);
    Task<IReadOnlyList<User>> ListUsersAsync();
    Task<User> UpdateUserAsync(Guid userId, string? firstName, string? middleName, string? lastName, string? email, string? employeeId, bool? isActive);
    Task<User> ActivateUserAsync(Guid userId);
    Task<User> DeactivateUserAsync(Guid userId);
    Task<PagedResult<User>> QueryUsersAsync(UserQueryOptions queryOptions);
}