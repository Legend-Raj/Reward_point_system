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
}
