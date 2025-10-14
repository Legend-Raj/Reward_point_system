using System;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

public sealed class LedgerEntry
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid? EventId { get; }
    public Guid? RedemptionRequestId { get; }
    public LedgerEntryType Type { get; }
    public int Points { get; }
    public DateTimeOffset Timestamp { get; }

    public LedgerEntry(
        Guid ledgerEntryId,
        Guid userId,
        LedgerEntryType type,
        int points,
        DateTimeOffset timestamp,
        Guid? eventId = null,
        Guid? redemptionRequestId = null)
    {
        if (ledgerEntryId == Guid.Empty)
        {
            throw new DomainException(DomainErrors.Points.TransactionIdRequired);
        }
        if (userId == Guid.Empty)
        {
            throw new DomainException(DomainErrors.Points.UserRequired);
        }
        if (points <= 0)
        {
            throw new DomainException(DomainErrors.Points.TransactionMustBePositive);
        }

        if (type == LedgerEntryType.Earn && eventId is null)
        {
            throw new DomainException(DomainErrors.Points.EarnRequiresEvent);
        }
        if (type == LedgerEntryType.Redeem && redemptionRequestId is null)
        {
            throw new DomainException(DomainErrors.Points.RedeemRequiresRedemptionRequest);
        }

    Id = ledgerEntryId;
        UserId = userId;
        Type = type;
        Points = points;
        Timestamp = timestamp;
        EventId = eventId;
        RedemptionRequestId = redemptionRequestId;
    }

    public override string ToString()
    {
        return $"Ledger [{Id}] for User [{UserId}] - {Type} {Points} pts at {Timestamp:o}";
    }
}
