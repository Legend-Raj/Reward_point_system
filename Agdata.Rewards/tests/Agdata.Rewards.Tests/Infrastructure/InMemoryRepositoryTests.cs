using System.Linq;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Infrastructure;

public class InMemoryRepositoryTests
{
    [Fact]
    public async Task UserRepository_ShouldAddAndRetrieveByEmail()
    {
        var repository = new UserRepositoryInMemory();
        var user = User.CreateNew("Nikhil Rao", "nikhil.rao@agdata.com", "AGD-450");
        repository.Add(user);

        var retrieved = await repository.GetByEmailAsync(user.Email);

        Assert.Equal(user.Id, retrieved!.Id);
    }

    [Fact]
    public async Task UserRepository_Update_ShouldPersistChanges()
    {
        var repository = new UserRepositoryInMemory();
        var user = User.CreateNew("Aria Benson", "aria.benson@agdata.com", "AGD-451");
        repository.Add(user);
        user.UpdateName("Aria Benson-Field Services");
        repository.Update(user);

        var retrieved = await repository.GetByIdAsync(user.Id);
        Assert.Equal("Aria Benson-Field Services", retrieved!.Name);
    }

    [Fact]
    public async Task UserRepository_GetByEmployeeId_ShouldReturnMatch()
    {
        var repository = new UserRepositoryInMemory();
        var user = User.CreateNew("Kaya Holmes", "kaya.holmes@agdata.com", "AGD-452");
        repository.Add(user);

        var retrieved = await repository.GetByEmployeeIdAsync(new EmployeeId("AGD-452"));

        Assert.Equal(user.Id, retrieved!.Id);
    }

    [Fact]
    public async Task EventRepository_ShouldUpdateEvent()
    {
        var repository = new EventRepositoryInMemory();
        var rewardEvent = Event.CreateNew("AGDATA Greenlight Seminar", DateTimeOffset.UtcNow);
        repository.Add(rewardEvent);
        rewardEvent.UpdateEventName("AGDATA Mega Seminar");
        repository.Update(rewardEvent);

        var retrieved = await repository.GetByIdAsync(rewardEvent.Id);
        Assert.Equal("AGDATA Mega Seminar", retrieved!.Name);
    }

    [Fact]
    public async Task ProductRepository_GetAll_ShouldReturnEntries()
    {
        var repository = new ProductRepositoryInMemory();
        repository.Add(Product.CreateNew("AGDATA Field Sticker", 50));
        repository.Add(Product.CreateNew("AGDATA Badge", 75));

        var all = await repository.GetAllAsync();
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task ProductRepository_Delete_ShouldRemoveProduct()
    {
        var repository = new ProductRepositoryInMemory();
        var product = Product.CreateNew("AGDATA Soil Kit", 600);
        repository.Add(product);

        repository.Delete(product.Id);

        var retrieved = await repository.GetByIdAsync(product.Id);
        Assert.Null(retrieved);
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
