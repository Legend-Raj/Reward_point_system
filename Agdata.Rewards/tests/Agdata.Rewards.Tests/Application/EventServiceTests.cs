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

    private static Admin CreateAdmin() => Admin.CreateNew("Coach", "coach@example.com", "ADMIN-5");

    [Fact]
    public async Task CreateEventAsync_ShouldStoreEvent()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);

        var eventId = await service.CreateEventAsync(CreateAdmin(), "Quiz", DateTimeOffset.UtcNow.AddDays(2));
        var stored = await repository.GetByIdAsync(eventId);

        Assert.NotNull(stored);
        Assert.Equal("Quiz", stored!.Name);
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldModifyNameAndDate()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var eventId = await service.CreateEventAsync(CreateAdmin(), "Quiz", DateTimeOffset.UtcNow);

        await service.UpdateEventAsync(CreateAdmin(), eventId, "Mega Quiz", DateTimeOffset.UtcNow.AddDays(1));
        var updated = await repository.GetByIdAsync(eventId);

        Assert.Equal("Mega Quiz", updated!.Name);
    }

    [Fact]
    public async Task DeactivateEventAsync_ShouldSetInactive()
    {
        var repository = new EventRepositoryInMemory();
        var service = BuildService(repository);
        var eventId = await service.CreateEventAsync(CreateAdmin(), "Hackathon", DateTimeOffset.UtcNow);

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
}
