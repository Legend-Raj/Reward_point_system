using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(Email email);
    void Add(User user);
    void Update(User user);
}