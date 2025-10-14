using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Agdata.Rewards.Tests.Common;
using Xunit;

namespace Agdata.Rewards.Tests.Infrastructure;

public class InMemoryRepositoryTests
{
    [Fact]
    public async Task UserRepository_ShouldAddAndRetrieveByEmail()
    {
        var repository = new UserRepositoryInMemory();
        var user = CreateUser("Nikhil Rao", "nikhil.rao@agdata.com", "AGD-450");
        repository.AddUser(user);

        var retrieved = await repository.GetUserByEmailAsync(user.Email);

        Assert.Equal(user.Id, retrieved!.Id);
    }

    [Fact]
    public async Task UserRepository_Update_ShouldPersistChanges()
    {
        var repository = new UserRepositoryInMemory();
        var user = CreateUser("Aria Benson", "aria.benson@agdata.com", "AGD-451");
        repository.AddUser(user);
    user.Rename(CreatePersonName("Aria Benson-Field Services"));
        repository.UpdateUser(user);

        var retrieved = await repository.GetUserByIdAsync(user.Id);
        Assert.Equal("Aria Benson-Field Services", retrieved!.Name.FullName);
    }

    [Fact]
    public async Task UserRepository_GetByEmployeeId_ShouldReturnMatch()
    {
        var repository = new UserRepositoryInMemory();
        var user = CreateUser("Kaya Holmes", "kaya.holmes@agdata.com", "AGD-452");
        repository.AddUser(user);

        var retrieved = await repository.GetUserByEmployeeIdAsync(new EmployeeId("AGD-452"));

        Assert.Equal(user.Id, retrieved!.Id);
    }

    [Fact]
    public async Task EventRepository_ShouldUpdateEvent()
    {
        var repository = new EventRepositoryInMemory();
        var occursAt = DateTimeOffset.UtcNow;
        var rewardEvent = Event.CreateNew("AGDATA Greenlight Seminar", occursAt);
        repository.AddEvent(rewardEvent);
        var updatedOccursAt = occursAt.AddDays(2);
    rewardEvent.AdjustDetails("AGDATA Mega Seminar", updatedOccursAt);
        repository.UpdateEvent(rewardEvent);

        var retrieved = await repository.GetEventByIdAsync(rewardEvent.Id);
        Assert.Equal("AGDATA Mega Seminar", retrieved!.Name);
        Assert.Equal(updatedOccursAt, retrieved.OccursAt);
    }

    [Fact]
    public async Task ProductRepository_GetAll_ShouldReturnEntries()
    {
        var repository = new ProductRepositoryInMemory();
        repository.AddProduct(Product.CreateNew("AGDATA Field Sticker", 50));
        repository.AddProduct(Product.CreateNew("AGDATA Badge", 75));

        var all = await repository.ListProductsAsync();
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task ProductRepository_Delete_ShouldRemoveProduct()
    {
        var repository = new ProductRepositoryInMemory();
        var product = Product.CreateNew("AGDATA Soil Kit", 600);
        repository.AddProduct(product);

        repository.DeleteProduct(product.Id);

        var retrieved = await repository.GetProductByIdAsync(product.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task RedemptionRequestRepository_ShouldDetectPending()
    {
        var repository = new RedemptionRequestRepositoryInMemory();
        var redemption = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        repository.AddRedemptionRequest(redemption);

        var hasPending = await repository.HasPendingRedemptionRequestForProductAsync(redemption.UserId, redemption.ProductId);
        Assert.True(hasPending);

        redemption.Approve();
        repository.UpdateRedemptionRequest(redemption);
        hasPending = await repository.HasPendingRedemptionRequestForProductAsync(redemption.UserId, redemption.ProductId);
        Assert.False(hasPending);
    }

    [Fact]
    public void LedgerEntryRepository_ShouldStoreEntries()
    {
        var repository = new LedgerEntryRepositoryInMemory();
        var entry = new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            LedgerEntryType.Earn,
            10,
            DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid());

        repository.AddLedgerEntry(entry);

        // No read API, so rely on ToString to ensure item exists by verifying it doesn't throw when enumerated indirectly via reflection.
        var field = typeof(LedgerEntryRepositoryInMemory)
            .GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = (List<LedgerEntry>)field!.GetValue(repository)!;
        Assert.Single(list);
    }

    private static User CreateUser(string fullName, string email, string employeeId)
    {
        var (first, middle, last) = NameTestHelper.Split(fullName);
        return User.CreateNew(first, middle, last, email, employeeId);
    }

    private static PersonName CreatePersonName(string fullName)
    {
        var (first, middle, last) = NameTestHelper.Split(fullName);
        return PersonName.Create(first, middle, last);
    }
}
