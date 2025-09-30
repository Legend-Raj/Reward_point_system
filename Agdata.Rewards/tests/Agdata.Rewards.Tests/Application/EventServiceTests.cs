using System.Linq;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class EventServiceTests
{
    private static EventService BuildService(EventRepositoryInMemory repository)
        => new(repository, new InMemoryUnitOfWork());

    private static Admin CreateAdmin() => Admin.CreateNew("Priya Malhotra", "priya.malhotra@agdata.com", "AGD-ADMIN-105");

    [Fact]
    public async Task CreateEventAsync_ShouldStoreEvent()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);

        var eventId = await service.CreateEventAsync(CreateAdmin(), "AGDATA Discovery Session", DateTimeOffset.UtcNow.AddDays(2));
        var stored = await repository.GetByIdAsync(eventId);

        Assert.NotNull(stored);
        Assert.Equal("AGDATA Discovery Session", stored!.Name);
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldModifyNameAndDate()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var eventId = await service.CreateEventAsync(CreateAdmin(), "AGDATA Field Forum", DateTimeOffset.UtcNow);

        await service.UpdateEventAsync(CreateAdmin(), eventId, "AGDATA Field Forum 2.0", DateTimeOffset.UtcNow.AddDays(1));
        var updated = await repository.GetByIdAsync(eventId);

        Assert.Equal("AGDATA Field Forum 2.0", updated!.Name);
    }

    [Fact]
    public async Task DeactivateEventAsync_ShouldSetInactive()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var eventId = await service.CreateEventAsync(CreateAdmin(), "AGDATA Hackathon", DateTimeOffset.UtcNow);

        await service.DeactivateEventAsync(CreateAdmin(), eventId);
        var updated = await repository.GetByIdAsync(eventId);

        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task UpdateEventAsync_WhenMissing_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateEventAsync(CreateAdmin(), Guid.NewGuid(), "", DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task DeactivateEventAsync_WhenMissing_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.DeactivateEventAsync(CreateAdmin(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateEventAsync_WithInvalidName_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.CreateEventAsync(CreateAdmin(), " ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task GetActiveEventsAsync_ShouldReturnUpcomingActiveEvents()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var now = DateTimeOffset.UtcNow;

        var activeFuture = Event.CreateNew("AGDATA Yield Summit", now.AddDays(5));
        var inactiveFuture = Event.CreateNew("Inactive Expo", now.AddDays(6));
        inactiveFuture.MakeInactive();
        var pastEvent = Event.CreateNew("Past Field Walk", now.AddDays(-2));

        repository.Add(activeFuture);
        repository.Add(inactiveFuture);
        repository.Add(pastEvent);

        var results = await service.GetActiveEventsAsync();

        Assert.Single(results);
        Assert.Equal(activeFuture.Id, results.First().Id);
    }

    [Fact]
    public async Task GetPastEventsAsync_ShouldReturnHistoricalEventsOrderedDescending()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var now = DateTimeOffset.UtcNow;

        var older = Event.CreateNew("Legacy Agronomy Talk", now.AddDays(-10));
        var recent = Event.CreateNew("Recent Dealer Brief", now.AddDays(-1));
        var future = Event.CreateNew("Up Next", now.AddDays(3));

        repository.Add(older);
        repository.Add(recent);
        repository.Add(future);

        var results = (await service.GetPastEventsAsync()).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(recent.Id, results[0].Id);
        Assert.Equal(older.Id, results[1].Id);
    }
}
