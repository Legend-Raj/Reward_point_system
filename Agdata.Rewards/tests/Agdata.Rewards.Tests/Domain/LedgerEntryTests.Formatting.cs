using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public partial class LedgerEntryTests
{
    [Fact]
    public void ToString_ShouldIncludeKeyFields()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var entry = new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            LedgerEntryType.Earn,
            15,
            timestamp,
            eventId: Guid.NewGuid());

        var text = entry.ToString();

        Assert.Contains(entry.Id.ToString(), text);
        Assert.Contains(entry.UserId.ToString(), text);
        Assert.Contains("Earn", text);
        Assert.Contains(entry.Points.ToString(), text);
        Assert.Contains(timestamp.ToString("o"), text);
    }

    [Fact]
    public void RedeemEntry_ShouldExposeOnlyRedemptionRequestId()
    {
        var redemptionRequestId = Guid.NewGuid();
        var entry = new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            LedgerEntryType.Redeem,
            40,
            DateTimeOffset.UtcNow,
            redemptionRequestId: redemptionRequestId);

        Assert.Equal(redemptionRequestId, entry.RedemptionRequestId);
        Assert.Null(entry.EventId);
    }
}
