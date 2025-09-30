using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class PointsTransactionTests
{
    [Fact]
    public void Ctor_ForEarnWithoutEvent_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => new PointsTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Earn,
            50,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Ctor_ForRedeemWithoutRedemption_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => new PointsTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Redeem,
            50,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Ctor_WithValidArguments_ShouldSucceed()
    {
        var transaction = new PointsTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Earn,
            25,
            DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid());

        Assert.Equal(TransactionType.Earn, transaction.Type);
        Assert.Equal(25, transaction.Points);
    }

    [Fact]
    public void Ctor_WithEmptyIdentifiers_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => new PointsTransaction(
            Guid.Empty,
            Guid.NewGuid(),
            TransactionType.Earn,
            10,
            DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid()));

        Assert.Throws<DomainException>(() => new PointsTransaction(
            Guid.NewGuid(),
            Guid.Empty,
            TransactionType.Earn,
            10,
            DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid()));
    }

    [Fact]
    public void Ctor_WithNonPositivePoints_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => new PointsTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Earn,
            0,
            DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid()));

        Assert.Throws<DomainException>(() => new PointsTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Redeem,
            -15,
            DateTimeOffset.UtcNow,
            redemptionId: Guid.NewGuid()));
    }

    [Fact]
    public void Ctor_ForRedeemWithRedemption_ShouldSucceed()
    {
        var redemptionId = Guid.NewGuid();
        var transaction = new PointsTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Redeem,
            40,
            DateTimeOffset.UtcNow,
            redemptionId: redemptionId);

        Assert.Equal(TransactionType.Redeem, transaction.Type);
        Assert.Equal(redemptionId, transaction.RedemptionId);
        Assert.Null(transaction.EventId);
    }
}
