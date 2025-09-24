using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

public sealed class PointsTransaction
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid? EventId { get; }
    public Guid? RedemptionId { get; }
    public TransactionType Type { get; }
    public int Points { get; }
    public DateTimeOffset Timestamp { get; }

    public PointsTransaction(
        Guid id,
        Guid userId,
        TransactionType type,
        int points,
        DateTimeOffset timestamp,
        Guid? eventId = null,
        Guid? redemptionId = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Transaction Id cannot be empty.");
        }
        if (userId == Guid.Empty)
        {
            throw new DomainException("UserId is required for a transaction.");
        }
        if (points <= 0)
        {
            throw new DomainException("Transaction points must be a positive number.");
        }

        if (type == TransactionType.Earn && eventId is null)
        {
            throw new DomainException("'Earn' transaction must be linked to an event.");
        }
        if (type == TransactionType.Redeem && redemptionId is null)
        {
            throw new DomainException("'Redeem' transaction must be linked to a redemption.");
        }

        Id = id;
        UserId = userId;
        Type = type;
        Points = points;
        Timestamp = timestamp;
        EventId = eventId;
        RedemptionId = redemptionId;
    }

    public override string ToString()
    {
        return $"Txn [{Id}] for User [{UserId}] - {Type} {Points} pts at {Timestamp:o}";
    }
}