using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class RedemptionServiceTests
{
    private sealed class CapturingPointsTransactionRepository : IPointsTransactionRepository
    {
        public List<PointsTransaction> Transactions { get; } = new();
        public void Add(PointsTransaction transaction) => Transactions.Add(transaction);
    }

    private static (RedemptionService service, UserRepositoryInMemory users, ProductRepositoryInMemory products, RedemptionRepositoryInMemory redemptions, CapturingPointsTransactionRepository txRepo)
        BuildService()
    {
        var userRepo = new UserRepositoryInMemory();
        var productRepo = new ProductRepositoryInMemory();
        var redemptionRepo = new RedemptionRepositoryInMemory();
        var txRepo = new CapturingPointsTransactionRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var service = new RedemptionService(userRepo, productRepo, redemptionRepo, txRepo, unitOfWork);
        return (service, userRepo, productRepo, redemptionRepo, txRepo);
    }

    private static Admin CreateAdmin() => Admin.CreateNew("Approver", "approver@example.com", "ADMIN-30");

    [Fact]
    public async Task RequestRedemptionAsync_ShouldLockPointsAndCreatePending()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = User.CreateNew("Student", "student@example.com", "EMP-41");
        user.AddPoints(1000);
        users.Add(user);
        var product = Product.CreateNew("Tablet", 500, stock: 5);
        products.Add(product);

        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        var storedRedemption = await redemptions.GetByIdAsync(redemptionId);
        Assert.NotNull(storedRedemption);
        Assert.Equal(RedemptionStatus.Pending, storedRedemption!.Status);
        Assert.Equal(500, (await users.GetByIdAsync(user.Id))!.LockedPoints);
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenDuplicatePending_ShouldThrow()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = User.CreateNew("Student", "student2@example.com", "EMP-42");
        user.AddPoints(1000);
        users.Add(user);
        var product = Product.CreateNew("Headphones", 400, stock: 5);
        products.Add(product);
        redemptions.Add(Redemption.CreateNew(user.Id, product.Id));

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenUserInactive_ShouldThrow()
    {
        var (service, users, products, _, _) = BuildService();
        var user = new User(Guid.NewGuid(), "Inactive", new("inactive@example.com"), new("EMP-46"), isActive: false, totalPoints: 500);
        users.Add(user);
        var product = Product.CreateNew("Lamp", 200, stock: 1);
        products.Add(product);

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenProductInactive_ShouldThrow()
    {
        var (service, users, products, _, _) = BuildService();
        var user = User.CreateNew("Student", "student6@example.com", "EMP-47");
        user.AddPoints(500);
        users.Add(user);
        var product = Product.CreateNew("Mouse", 150, stock: 1);
        product.MakeInactive();
        products.Add(product);

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_ShouldCommitPointsReduceStockAndLog()
    {
        var (service, users, products, redemptions, txRepo) = BuildService();
        var user = User.CreateNew("Student", "student3@example.com", "EMP-43");
        user.AddPoints(1000);
        users.Add(user);
        var product = Product.CreateNew("Smart Watch", 300, stock: 2);
        products.Add(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);
    await service.ApproveRedemptionAsync(CreateAdmin(), redemptionId);

    await service.DeliverRedemptionAsync(CreateAdmin(), redemptionId);

        var updatedUser = await users.GetByIdAsync(user.Id);
        var updatedProduct = await products.GetByIdAsync(product.Id);
        var updatedRedemption = await redemptions.GetByIdAsync(redemptionId);

        Assert.Equal(700, updatedUser!.TotalPoints);
        Assert.Equal(0, updatedUser.LockedPoints);
        Assert.Equal(1, updatedProduct!.Stock);
        Assert.Equal(RedemptionStatus.Delivered, updatedRedemption!.Status);
        Assert.Single(txRepo.Transactions);
        Assert.Equal(TransactionType.Redeem, txRepo.Transactions[0].Type);
    }

    [Fact]
    public async Task RejectRedemptionAsync_ShouldUnlockPoints()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = User.CreateNew("Student", "student4@example.com", "EMP-44");
        user.AddPoints(600);
        users.Add(user);
        var product = Product.CreateNew("Gift Card", 300, stock: null);
        products.Add(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

    await service.RejectRedemptionAsync(CreateAdmin(), redemptionId);

        var updatedUser = await users.GetByIdAsync(user.Id);
        var redemption = await redemptions.GetByIdAsync(redemptionId);
        Assert.Equal(0, updatedUser!.LockedPoints);
        Assert.Equal(RedemptionStatus.Rejected, redemption!.Status);
    }

    [Fact]
    public async Task CancelRedemptionAsync_ShouldUnlockPoints()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = User.CreateNew("Student", "student5@example.com", "EMP-45");
        user.AddPoints(400);
        users.Add(user);
        var product = Product.CreateNew("Gaming Voucher", 200, stock: null);
        products.Add(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

    await service.CancelRedemptionAsync(CreateAdmin(), redemptionId);

        var updatedUser = await users.GetByIdAsync(user.Id);
        var redemption = await redemptions.GetByIdAsync(redemptionId);
        Assert.Equal(0, updatedUser!.LockedPoints);
        Assert.Equal(RedemptionStatus.Canceled, redemption!.Status);
    }
}
