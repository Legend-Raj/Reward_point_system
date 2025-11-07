using System;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IRedemptionRequestRepository
{
    Task<RedemptionRequest?> GetRedemptionRequestByIdAsync(Guid redemptionRequestId, CancellationToken cancellationToken = default);
    Task<RedemptionRequest?> GetRedemptionRequestByIdForUpdateAsync(Guid redemptionRequestId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingRedemptionRequestForProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<bool> AnyPendingRedemptionRequestsForProductAsync(Guid productId, CancellationToken cancellationToken = default);
    void AddRedemptionRequest(RedemptionRequest redemptionRequest);
    void UpdateRedemptionRequest(RedemptionRequest redemptionRequest);
}

