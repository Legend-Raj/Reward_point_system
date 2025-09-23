using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

/// <summary>
/// Represents a user's request to redeem points for a product.
/// This entity acts as a state machine, ensuring the redemption process
/// follows valid lifecycle transitions (e.g., Pending -> Approved -> Delivered).
/// </summary>
public sealed class Redemption
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid ProductId { get; }

    public RedemptionStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }

    private Redemption(Guid id, Guid userId, Guid productId)
    {
        if (id == Guid.Empty) 
        {
            throw new DomainException("Redemption Id cannot be empty.");
        }
        if (userId == Guid.Empty) 
        {
            throw new DomainException("UserId is required for a redemption.");
        }
        if (productId == Guid.Empty) 
        {
            throw new DomainException("ProductId is required for a redemption.");
        }

        Id = id;
        UserId = userId;
        ProductId = productId;
        Status = RedemptionStatus.Pending;
        RequestedAt = DateTimeOffset.UtcNow;
    }

    public static Redemption CreateNew(Guid userId, Guid productId)
    {
        return new Redemption(Guid.NewGuid(), userId, productId);
    }

    public void Approve()
    {
        if (Status != RedemptionStatus.Pending)
        {
            throw new DomainException("Only a 'Pending' redemption can be approved.");
        }
        Status = RedemptionStatus.Approved;
        ApprovedAt = DateTimeOffset.UtcNow;
    }

    public void Deliver()
    {
        if (Status != RedemptionStatus.Approved)
        {
            throw new DomainException("Only an 'Approved' redemption can be delivered.");
        }
        Status = RedemptionStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
    }

    public void Reject()
    {
        if (Status != RedemptionStatus.Pending)
        {
            throw new DomainException("Only a 'Pending' redemption can be rejected.");
        }
        Status = RedemptionStatus.Rejected;
    }

    public void Cancel()
    {
        if (Status != RedemptionStatus.Pending)
        {
            throw new DomainException("Only a 'Pending' redemption can be canceled.");
        }
        Status = RedemptionStatus.Canceled;
    }

    public override string ToString()
        => $"Redemption [{Id}] for User [{UserId}] - Status: {Status}";
}