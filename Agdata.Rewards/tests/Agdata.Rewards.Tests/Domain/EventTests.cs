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
}
