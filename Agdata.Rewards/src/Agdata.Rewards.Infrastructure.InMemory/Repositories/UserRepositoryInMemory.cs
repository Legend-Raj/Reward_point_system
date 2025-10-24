using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class UserRepositoryInMemory : IUserRepository
{
    
    private readonly Dictionary<Guid, User> _users = new();
    private readonly object _gate = new();

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }
    }

    public Task<User?> GetUserByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var user = _users.Values.FirstOrDefault(u => u.Email.Value == email.Value);
            return Task.FromResult(user);
        }
    }

    public Task<User?> GetUserByEmployeeIdAsync(EmployeeId employeeId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var user = _users.Values.FirstOrDefault(u => u.EmployeeId.Value == employeeId.Value);
            return Task.FromResult(user);
        }
    }

    public void AddUser(User user)
    {
        lock (_gate)
        {
            _users[user.Id] = user;
        }
    }

    public void UpdateUser(User user)
    {
        lock (_gate)
        {
            _users[user.Id] = user;
        }
    }

    public Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            IReadOnlyList<User> users = _users.Values.ToList();
            return Task.FromResult(users);
        }
    }
}