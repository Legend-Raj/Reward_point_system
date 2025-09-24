using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class RedemptionRepositoryInMemory : IRedemptionRepository
{
    private readonly Dictionary<Guid, Redemption> _redemptions = new();

    public Task<Redemption?> GetByIdAsync(Guid id)
    {
        _redemptions.TryGetValue(id, out var redemption);
        return Task.FromResult(redemption);
    }

    public Task<bool> HasPendingRedemptionForProductAsync(Guid userId, Guid productId)
    {
        var hasPending = _redemptions.Values.Any(r =>
            r.UserId == userId &&
            r.ProductId == productId &&
            r.Status == RedemptionStatus.Pending);

        return Task.FromResult(hasPending);
    }

    public Task<bool> AnyPendingRedemptionsForProductAsync(Guid productId)
    {
        var anyPending = _redemptions.Values.Any(r =>
            r.ProductId == productId &&
            r.Status == RedemptionStatus.Pending);

        return Task.FromResult(anyPending);
    }

    public void Add(Redemption redemption)
    {
        _redemptions[redemption.Id] = redemption;
    }

    public void Update(Redemption redemption)
    {
        _redemptions[redemption.Id] = redemption;
    }
}