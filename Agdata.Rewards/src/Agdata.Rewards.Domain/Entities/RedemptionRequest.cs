using System;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

public class RedemptionRequest
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid ProductId { get; }

    public RedemptionRequestStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }

    protected RedemptionRequest()
    {
        Id = Guid.Empty;
        UserId = Guid.Empty;
        ProductId = Guid.Empty;
        Status = RedemptionRequestStatus.Pending;
        RequestedAt = DateTimeOffset.UtcNow;
    }

    protected RedemptionRequest(Guid requestId, Guid userId, Guid productId)
    {
        if (requestId == Guid.Empty)
        {
            throw new DomainException(DomainErrors.RedemptionRequest.IdRequired);
        }
        if (userId == Guid.Empty)
        {
            throw new DomainException(DomainErrors.RedemptionRequest.UserRequired);
        }
        if (productId == Guid.Empty)
        {
            throw new DomainException(DomainErrors.RedemptionRequest.ProductRequired);
        }

        Id = requestId;
        UserId = userId;
        ProductId = productId;
        Status = RedemptionRequestStatus.Pending;
        RequestedAt = DateTimeOffset.UtcNow;
    }

    public static RedemptionRequest CreateNew(Guid userId, Guid productId)
    {
        return new RedemptionRequest(Guid.NewGuid(), userId, productId);
    }

    public void Approve()
    {
        if (Status != RedemptionRequestStatus.Pending)
        {
            throw new DomainException(DomainErrors.RedemptionRequest.ApproveRequiresPending);
        }
        Status = RedemptionRequestStatus.Approved;
        ApprovedAt = DateTimeOffset.UtcNow;
    }

    public void Deliver()
    {
        if (Status != RedemptionRequestStatus.Approved)
        {
            throw new DomainException(DomainErrors.RedemptionRequest.DeliverRequiresApproved);
        }
        Status = RedemptionRequestStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
    }

    public void Reject()
    {
        if (Status != RedemptionRequestStatus.Pending)
        {
            throw new DomainException(DomainErrors.RedemptionRequest.RejectRequiresPending);
        }
        Status = RedemptionRequestStatus.Rejected;
    }

    public void Cancel()
    {
        if (Status != RedemptionRequestStatus.Pending)
        {
            throw new DomainException(DomainErrors.RedemptionRequest.CancelRequiresPending);
        }
        Status = RedemptionRequestStatus.Canceled;
    }

    public override string ToString()
    => $"RedemptionRequest [{Id}] for User [{UserId}] - Status: {Status}";
}
