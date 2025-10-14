using System;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IRedemptionRequestRepository
{
    /// <summary>Fetches a redemption request by its identifier.</summary>
    Task<RedemptionRequest?> GetRedemptionRequestByIdAsync(Guid redemptionRequestId);

    /// <summary>Determines whether the user already has a pending request for the product.</summary>
    Task<bool> HasPendingRedemptionRequestForProductAsync(Guid userId, Guid productId);

    /// <summary>Checks if any pending requests exist for a product.</summary>
    Task<bool> AnyPendingRedemptionRequestsForProductAsync(Guid productId);

    /// <summary>Persists a newly submitted redemption request.</summary>
    void AddRedemptionRequest(RedemptionRequest redemptionRequest);

    /// <summary>Persists updates to an existing redemption request.</summary>
    void UpdateRedemptionRequest(RedemptionRequest redemptionRequest);
}

