using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class EventTests
{
    [Fact]
    public void CreateNew_ShouldBeActiveByDefault()
    {
        var when = DateTimeOffset.UtcNow.AddDays(1);
        var rewardEvent = Event.CreateNew("Hackathon Finals", when);

        Assert.Equal("Hackathon Finals", rewardEvent.Name);
    Assert.Equal(when, rewardEvent.OccursAt);
        Assert.True(rewardEvent.IsActive);
    }

    [Fact]
    public void MakeInactive_ShouldToggleFlag()
    {
        var rewardEvent = Event.CreateNew("Townhall", DateTimeOffset.UtcNow);

        rewardEvent.MakeInactive();

        Assert.False(rewardEvent.IsActive);
    }

    [Fact]
    public void AdjustDetails_ShouldValidateName_AndPersistChanges()
    {
        var originalWhen = DateTimeOffset.UtcNow;
        var rewardEvent = Event.CreateNew("Old", originalWhen);

        var newWhen = originalWhen.AddDays(7);
    rewardEvent.AdjustDetails("New Title", newWhen);

        Assert.Equal("New Title", rewardEvent.Name);
        Assert.Equal(newWhen, rewardEvent.OccursAt);

    Assert.Throws<DomainException>(() => rewardEvent.AdjustDetails(" ", newWhen));
    }

    [Fact]
    public void CreateNew_WithBlankName_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Event.CreateNew(" ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Ctor_WithEmptyId_ShouldThrow()
    {
    Assert.Throws<DomainException>(() => new Event(Guid.Empty, "Dealer Workshop", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AdjustDetails_ShouldTrimName()
    {
        var originalWhen = DateTimeOffset.UtcNow;
        var rewardEvent = Event.CreateNew("Quarterly Brief", originalWhen);

        var updatedWhen = originalWhen.AddHours(3);
    rewardEvent.AdjustDetails("  AGDATA Roadshow  ", updatedWhen);

        Assert.Equal("AGDATA Roadshow", rewardEvent.Name);
        Assert.Equal(updatedWhen, rewardEvent.OccursAt);
    }

    [Fact]
    public void MakeActive_ShouldReactivateEvent()
    {
        var rewardEvent = Event.CreateNew("Legacy Expo", DateTimeOffset.UtcNow);
        rewardEvent.MakeInactive();

        rewardEvent.MakeActive();

        Assert.True(rewardEvent.IsActive);
    }
}
