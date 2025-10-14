using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public partial class LedgerEntryTests
{
    [Fact]
    public void Ctor_ForEarnWithoutEvent_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            LedgerEntryType.Earn,
            50,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Ctor_ForRedeemWithoutRequest_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            LedgerEntryType.Redeem,
            50,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Ctor_WithValidArguments_ShouldSucceed()
    {
        var entry = new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            LedgerEntryType.Earn,
            25,
            DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid());

        Assert.Equal(LedgerEntryType.Earn, entry.Type);
        Assert.Equal(25, entry.Points);
    }

    [Fact]
    public void Ctor_WithEmptyIdentifiers_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => new LedgerEntry(
            Guid.Empty,
            Guid.NewGuid(),
            LedgerEntryType.Earn,
            10,
            DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid()));

        Assert.Throws<DomainException>(() => new LedgerEntry(
            Guid.NewGuid(),
            Guid.Empty,
            LedgerEntryType.Earn,
            10,
            DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid()));
    }

    [Fact]
    public void Ctor_WithNonPositivePoints_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            LedgerEntryType.Earn,
            0,
            DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid()));

        Assert.Throws<DomainException>(() => new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            LedgerEntryType.Redeem,
            -15,
            DateTimeOffset.UtcNow,
            redemptionRequestId: Guid.NewGuid()));
    }

    [Fact]
    public void Ctor_ForRedeemWithRedemption_ShouldSucceed()
    {
        var redemptionRequestId = Guid.NewGuid();
        var entry = new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            LedgerEntryType.Redeem,
            40,
            DateTimeOffset.UtcNow,
            redemptionRequestId: redemptionRequestId);

        Assert.Equal(LedgerEntryType.Redeem, entry.Type);
        Assert.Equal(redemptionRequestId, entry.RedemptionRequestId);
        Assert.Null(entry.EventId);
    }
}
