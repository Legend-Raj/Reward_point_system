using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class RedemptionRequestRepositoryInMemory : IRedemptionRequestRepository
{
    private readonly Dictionary<Guid, RedemptionRequest> _redemptions = new();
    private readonly object _gate = new();

    public Task<RedemptionRequest?> GetRedemptionRequestByIdAsync(Guid redemptionRequestId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _redemptions.TryGetValue(redemptionRequestId, out var redemption);
            return Task.FromResult(redemption);
        }
    }

    public Task<RedemptionRequest?> GetRedemptionRequestByIdForUpdateAsync(Guid redemptionRequestId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _redemptions.TryGetValue(redemptionRequestId, out var redemption);
            return Task.FromResult(redemption);
        }
    }

    public Task<bool> HasPendingRedemptionRequestForProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var hasPending = _redemptions.Values.Any(r =>
                r.UserId == userId &&
                r.ProductId == productId &&
                r.Status == RedemptionRequestStatus.Pending);

            return Task.FromResult(hasPending);
        }
    }

    public Task<bool> AnyPendingRedemptionRequestsForProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var anyPending = _redemptions.Values.Any(r =>
                r.ProductId == productId &&
                r.Status == RedemptionRequestStatus.Pending);

            return Task.FromResult(anyPending);
        }
    }

    public void AddRedemptionRequest(RedemptionRequest redemptionRequest)
    {
        lock (_gate)
        {
            _redemptions[redemptionRequest.Id] = redemptionRequest;
        }
    }

    public void UpdateRedemptionRequest(RedemptionRequest redemptionRequest)
    {
        lock (_gate)
        {
            _redemptions[redemptionRequest.Id] = redemptionRequest;
        }
    }
}
