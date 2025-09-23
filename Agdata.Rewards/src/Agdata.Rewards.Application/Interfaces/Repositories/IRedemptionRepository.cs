using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IRedemptionRepository
{
    Task<Redemption?> GetByIdAsync(Guid id);
    Task<bool> HasPendingRedemptionForProductAsync(Guid userId, Guid productId);
    void Add(Redemption redemption);
    void Update(Redemption redemption);
}