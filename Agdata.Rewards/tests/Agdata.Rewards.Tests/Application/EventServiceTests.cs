using System.Linq;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Agdata.Rewards.Tests.Common;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class EventServiceTests
{
    private static EventService BuildService(EventRepositoryInMemory repository)
        => new(repository, new InMemoryUnitOfWork());

    private static Admin CreateAdmin()
    {
        var (first, middle, last) = NameTestHelper.Split("Priya Malhotra");
        return Admin.CreateNew(first, middle, last, "priya.malhotra@agdata.com", "AGD-105");
    }

    private static Admin CreateInactiveAdmin()
    {
        var admin = CreateAdmin();
        admin.Deactivate();
        return admin;
    }

    [Fact]
    public async Task CreateEventAsync_ShouldStoreEvent()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);

        var occursAt = DateTimeOffset.UtcNow.AddDays(2);
        var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Discovery Session", occursAt);
        var stored = await repository.GetEventByIdAsync(created.Id);

        Assert.NotNull(stored);
        Assert.Equal("AGDATA Discovery Session", stored!.Name);
        Assert.Equal(occursAt, stored.OccursAt);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldModifyNameAndDate()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var initialOccursAt = DateTimeOffset.UtcNow;
        var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Field Forum", initialOccursAt);

        var updatedOccursAt = initialOccursAt.AddDays(1);
        var updatedResult = await service.UpdateEventAsync(CreateAdmin(), created.Id, "AGDATA Field Forum 2.0", updatedOccursAt);
        var updated = await repository.GetEventByIdAsync(created.Id);

        Assert.Equal("AGDATA Field Forum 2.0", updated!.Name);
    Assert.Equal(updatedOccursAt, updated.OccursAt);
        Assert.Equal(updatedOccursAt, updatedResult.OccursAt);
    }

    [Fact]
    public async Task DeactivateEventAsync_ShouldSetInactive()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Hackathon", DateTimeOffset.UtcNow);

        await service.DeactivateEventAsync(CreateAdmin(), created.Id);
        var updated = await repository.GetEventByIdAsync(created.Id);

        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task UpdateEventAsync_WhenMissing_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateEventAsync(CreateAdmin(), Guid.NewGuid(), "Nonexistent", DateTimeOffset.UtcNow));
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
    public async Task CreateEventAsync_WhenMarkedInactive_ShouldStoreInactiveEvent()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);

        var occursAt = DateTimeOffset.UtcNow.AddDays(4);
        var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Research Summit", occursAt, isActive: false);
        var stored = await repository.GetEventByIdAsync(created.Id);

        Assert.NotNull(stored);
        Assert.False(stored!.IsActive);
    }

    [Fact]
    public async Task CreateEventAsync_WithNullAdmin_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.CreateEventAsync(null!, "Quarterly Update", DateTimeOffset.UtcNow.AddDays(1)));
    }

    [Fact]
    public async Task CreateEventAsync_WhenAdminInactive_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.CreateEventAsync(CreateInactiveAdmin(), "Dealer Brief", DateTimeOffset.UtcNow.AddDays(1)));
    }

    [Fact]
    public async Task CreateEventAsync_WhenOccursAtDefault_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.CreateEventAsync(CreateAdmin(), "Dealer Brief", default));
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

        repository.AddEvent(activeFuture);
        repository.AddEvent(inactiveFuture);
        repository.AddEvent(pastEvent);

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

        repository.AddEvent(older);
        repository.AddEvent(recent);
        repository.AddEvent(future);

        var results = (await service.GetPastEventsAsync()).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(recent.Id, results[0].Id);
        Assert.Equal(older.Id, results[1].Id);
    }

    [Fact]
    public async Task UpdateEventAsync_WithNullAdmin_ShouldThrow()
    {
        var repository = new EventRepositoryInMemory();
    var service = BuildService(repository);
    var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Field Forum", DateTimeOffset.UtcNow.AddDays(2));

    await Assert.ThrowsAsync<DomainException>(() => service.UpdateEventAsync(null!, created.Id, "Renamed", DateTimeOffset.UtcNow.AddDays(3)));
    }

    [Fact]
    public async Task UpdateEventAsync_WhenAdminInactive_ShouldThrow()
    {
        var repository = new EventRepositoryInMemory();
    var service = BuildService(repository);
    var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Field Forum", DateTimeOffset.UtcNow.AddDays(2));

    await Assert.ThrowsAsync<DomainException>(() => service.UpdateEventAsync(CreateInactiveAdmin(), created.Id, "Renamed", DateTimeOffset.UtcNow.AddDays(3)));
    }

    [Fact]
    public async Task UpdateEventAsync_WhenEventIdEmpty_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateEventAsync(CreateAdmin(), Guid.Empty, "Renamed", DateTimeOffset.UtcNow.AddDays(3)));
    }

    [Fact]
    public async Task UpdateEventAsync_WhenOccursAtDefault_ShouldThrow()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Field Forum", DateTimeOffset.UtcNow.AddDays(2));

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateEventAsync(CreateAdmin(), created.Id, "Renamed", default));
    }

    [Fact]
    public async Task SetEventActiveStateAsync_ShouldToggleState()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Dealer Rally", DateTimeOffset.UtcNow.AddDays(3), isActive: false);

        var activatedResult = await service.SetEventActiveStateAsync(CreateAdmin(), created.Id, true);
        var activated = await repository.GetEventByIdAsync(created.Id);
        Assert.True(activated!.IsActive);
        Assert.True(activatedResult.IsActive);

        var deactivatedResult = await service.SetEventActiveStateAsync(CreateAdmin(), created.Id, false);
        var deactivated = await repository.GetEventByIdAsync(created.Id);
        Assert.False(deactivated!.IsActive);
        Assert.False(deactivatedResult.IsActive);
    }

    [Fact]
    public async Task SetEventActiveStateAsync_WhenEventMissing_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.SetEventActiveStateAsync(CreateAdmin(), Guid.NewGuid(), true));
    }

    [Fact]
    public async Task SetEventActiveStateAsync_WhenAdminInvalid_ShouldThrow()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Grower Workshop", DateTimeOffset.UtcNow.AddDays(5));

        await Assert.ThrowsAsync<DomainException>(() => service.SetEventActiveStateAsync(null!, created.Id, true));
        await Assert.ThrowsAsync<DomainException>(() => service.SetEventActiveStateAsync(CreateInactiveAdmin(), created.Id, true));
    }

    [Fact]
    public async Task SetEventActiveStateAsync_WhenEventIdEmpty_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.SetEventActiveStateAsync(CreateAdmin(), Guid.Empty, true));
    }

    [Fact]
    public async Task GetEventByIdAsync_ShouldReturnEventWhenExists()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Innovation Sprint", DateTimeOffset.UtcNow.AddDays(7));

        var result = await service.GetEventByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
    }

    [Fact]
    public async Task GetEventByIdAsync_WhenMissing_ShouldReturnNull()
    {
        var service = BuildService(new EventRepositoryInMemory());

        var result = await service.GetEventByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllEventsAsync_ShouldReturnActiveAndInactiveOrdered()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var first = await service.CreateEventAsync(CreateAdmin(), "Earlier", DateTimeOffset.UtcNow.AddDays(1));
        var second = await service.CreateEventAsync(CreateAdmin(), "Later", DateTimeOffset.UtcNow.AddDays(5), isActive: false);

        var results = (await service.GetAllEventsAsync()).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(first.Id, results[0].Id);
        Assert.Equal(second.Id, results[1].Id);
    }

    [Fact]
    public async Task DeactivateEventAsync_WithNullAdmin_ShouldThrow()
    {
    var repository = new EventRepositoryInMemory();
    var service = BuildService(repository);
    var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Hackathon", DateTimeOffset.UtcNow.AddDays(1));

    await Assert.ThrowsAsync<DomainException>(() => service.DeactivateEventAsync(null!, created.Id));
    }

    [Fact]
    public async Task DeactivateEventAsync_WhenAdminInactive_ShouldThrow()
    {
    var repository = new EventRepositoryInMemory();
    var service = BuildService(repository);
    var created = await service.CreateEventAsync(CreateAdmin(), "AGDATA Hackathon", DateTimeOffset.UtcNow.AddDays(1));

    await Assert.ThrowsAsync<DomainException>(() => service.DeactivateEventAsync(CreateInactiveAdmin(), created.Id));
    }

    [Fact]
    public async Task DeactivateEventAsync_WhenEventIdEmpty_ShouldThrow()
    {
        var service = BuildService(new EventRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.DeactivateEventAsync(CreateAdmin(), Guid.Empty));
    }
}
