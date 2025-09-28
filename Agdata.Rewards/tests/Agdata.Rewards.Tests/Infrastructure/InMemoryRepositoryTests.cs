using System.Linq;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Infrastructure;

public class InMemoryRepositoryTests
{
    [Fact]
    public async Task UserRepository_ShouldAddAndRetrieveByEmail()
    {
        var repository = new UserRepositoryInMemory();
        var user = User.CreateNew("Learner", "learner@example.com", "EMP-50");
        repository.Add(user);

        var retrieved = await repository.GetByEmailAsync(user.Email);

        Assert.Equal(user.Id, retrieved!.Id);
    }

    [Fact]
    public async Task UserRepository_Update_ShouldPersistChanges()
    {
        var repository = new UserRepositoryInMemory();
        var user = User.CreateNew("Learner", "learner2@example.com", "EMP-51");
        repository.Add(user);
        user.UpdateName("Advanced Learner");
        repository.Update(user);

        var retrieved = await repository.GetByIdAsync(user.Id);
        Assert.Equal("Advanced Learner", retrieved!.Name);
    }

    [Fact]
    public async Task EventRepository_ShouldUpdateEvent()
    {
        var repository = new EventRepositoryInMemory();
        var rewardEvent = Event.CreateNew("Seminar", DateTimeOffset.UtcNow);
        repository.Add(rewardEvent);
        rewardEvent.UpdateEventName("Mega Seminar");
        repository.Update(rewardEvent);

        var retrieved = await repository.GetByIdAsync(rewardEvent.Id);
        Assert.Equal("Mega Seminar", retrieved!.Name);
    }

    [Fact]
    public async Task ProductRepository_GetAll_ShouldReturnEntries()
    {
        var repository = new ProductRepositoryInMemory();
        repository.Add(Product.CreateNew("Sticker", 50));
        repository.Add(Product.CreateNew("Badge", 75));

        var all = await repository.GetAllAsync();
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task RedemptionRepository_ShouldDetectPending()
    {
        var repository = new RedemptionRepositoryInMemory();
        var redemption = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        repository.Add(redemption);

        var hasPending = await repository.HasPendingRedemptionForProductAsync(redemption.UserId, redemption.ProductId);
        Assert.True(hasPending);

        redemption.Approve();
        repository.Update(redemption);
        hasPending = await repository.HasPendingRedemptionForProductAsync(redemption.UserId, redemption.ProductId);
        Assert.False(hasPending);
    }

    [Fact]
    public void PointsTransactionRepository_ShouldStoreTransactions()
    {
        var repository = new PointsTransactionRepositoryInMemory();
        var transaction = new PointsTransaction(Guid.NewGuid(), Guid.NewGuid(), TransactionType.Earn, 10, DateTimeOffset.UtcNow, eventId: Guid.NewGuid());

        repository.Add(transaction);

        // No read API, so rely on ToString to ensure item exists by verifying it doesn't throw when enumerated indirectly via reflection.
        var field = typeof(PointsTransactionRepositoryInMemory)
            .GetField("_transactions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = (List<PointsTransaction>)field!.GetValue(repository)!;
        Assert.Single(list);
    }
}
