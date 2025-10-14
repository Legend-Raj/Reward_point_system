using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class UserRepositoryInMemory : IUserRepository
{
    // Dictionary keyed by user id keeps lookups O(1) for tests without the overhead of a relational store.
    private readonly Dictionary<Guid, User> _users = new();

    public Task<User?> GetUserByIdAsync(Guid userId)
    {
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetUserByEmailAsync(Email email)
    {
        var user = _users.Values.FirstOrDefault(u => u.Email.Value == email.Value);
        return Task.FromResult(user);
    }

    public Task<User?> GetUserByEmployeeIdAsync(EmployeeId employeeId)
    {
        var user = _users.Values.FirstOrDefault(u => u.EmployeeId.Value == employeeId.Value);
        return Task.FromResult(user);
    }

    public void AddUser(User user)
    {
        _users[user.Id] = user;
    }

    public void UpdateUser(User user)
    {
        _users[user.Id] = user;
    }

    public Task<IReadOnlyList<User>> ListUsersAsync()
    {
        IReadOnlyList<User> users = _users.Values.ToList();
        return Task.FromResult(users);
    }
}