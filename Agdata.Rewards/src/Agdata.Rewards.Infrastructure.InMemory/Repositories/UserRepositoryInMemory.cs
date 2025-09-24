using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class UserRepositoryInMemory : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new();

    public Task<User?> GetByIdAsync(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(Email email)
    {
        var user = _users.Values.FirstOrDefault(u => u.Email.Value == email.Value);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmployeeIdAsync(EmployeeId employeeId)
    {
        var user = _users.Values.FirstOrDefault(u => u.EmployeeId.Value == employeeId.Value);
        return Task.FromResult(user);
    }

    public void Add(User user)
    {
        _users[user.Id] = user;
    }

    public void Update(User user)
    {
        _users[user.Id] = user;
    }
}