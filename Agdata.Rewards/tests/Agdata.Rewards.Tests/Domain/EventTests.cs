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
        Assert.Equal(when, rewardEvent.OccurredAt);
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
    public void UpdateEventName_ShouldValidateInput()
    {
        var rewardEvent = Event.CreateNew("Old", DateTimeOffset.UtcNow);

        rewardEvent.UpdateEventName("New Title");
        Assert.Equal("New Title", rewardEvent.Name);

        Assert.Throws<DomainException>(() => rewardEvent.UpdateEventName(" "));
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
    public void UpdateEventName_ShouldTrimInput()
    {
        var rewardEvent = Event.CreateNew("Quarterly Brief", DateTimeOffset.UtcNow);

        rewardEvent.UpdateEventName("  AGDATA Roadshow  ");

        Assert.Equal("AGDATA Roadshow", rewardEvent.Name);
    }

    [Fact]
    public void ChangeEventDateTime_ShouldPersistNewTimestamp()
    {
        var rewardEvent = Event.CreateNew("Dealer Summit", DateTimeOffset.UtcNow);
        var newSchedule = DateTimeOffset.UtcNow.AddMonths(1);

        rewardEvent.ChangeEventDateTime(newSchedule);

        Assert.Equal(newSchedule, rewardEvent.OccurredAt);
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
