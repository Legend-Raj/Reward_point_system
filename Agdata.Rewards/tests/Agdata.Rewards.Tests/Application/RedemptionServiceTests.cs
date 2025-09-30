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

    private static Admin CreateAdmin() => Admin.CreateNew("Victor Alvarez", "victor.alvarez@agdata.com", "AGD-ADMIN-320");

    [Fact]
    public async Task RequestRedemptionAsync_ShouldLockPointsAndCreatePending()
    {
        var (service, users, products, redemptions, _) = BuildService();
    var user = User.CreateNew("Jada Holmes", "jada.holmes@agdata.com", "AGD-241");
        user.AddPoints(1000);
        users.Add(user);
    var product = Product.CreateNew("AGDATA Scout Tablet", 500, stock: 5);
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
        var user = User.CreateNew("Noah Kim", "noah.kim@agdata.com", "AGD-242");
        user.AddPoints(1000);
        users.Add(user);
        var product = Product.CreateNew("AGDATA Field Headset", 400, stock: 5);
        products.Add(product);
        redemptions.Add(Redemption.CreateNew(user.Id, product.Id));

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenUserInactive_ShouldThrow()
    {
        var (service, users, products, _, _) = BuildService();
        var user = new User(Guid.NewGuid(), "Inactive Agronomist", new("inactive@agdata.com"), new("AGD-246"), isActive: false, totalPoints: 500);
        users.Add(user);
        var product = Product.CreateNew("AGDATA Lamp", 200, stock: 1);
        products.Add(product);

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenProductInactive_ShouldThrow()
    {
        var (service, users, products, _, _) = BuildService();
        var user = User.CreateNew("Zuri Carson", "zuri.carson@agdata.com", "AGD-247");
        user.AddPoints(500);
        users.Add(user);
        var product = Product.CreateNew("AGDATA Precision Mouse", 150, stock: 1);
        product.MakeInactive();
        products.Add(product);

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenInsufficientPoints_ShouldThrow()
    {
        var (service, users, products, _, _) = BuildService();
        var user = User.CreateNew("Reese Patel", "reese.patel@agdata.com", "AGD-248");
        user.AddPoints(100);
        users.Add(user);
        var product = Product.CreateNew("AGDATA Drone Kit", 600, stock: 2);
        products.Add(product);

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_ShouldCommitPointsReduceStockAndLog()
    {
        var (service, users, products, redemptions, txRepo) = BuildService();
        var user = User.CreateNew("Elena Brooks", "elena.brooks@agdata.com", "AGD-243");
        user.AddPoints(1000);
        users.Add(user);
        var product = Product.CreateNew("AGDATA Smart Watch", 300, stock: 2);
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
        var user = User.CreateNew("Marcus Lane", "marcus.lane@agdata.com", "AGD-244");
        user.AddPoints(600);
        users.Add(user);
        var product = Product.CreateNew("AGDATA Gift Card", 300, stock: null);
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
        var user = User.CreateNew("Sofia Quinn", "sofia.quinn@agdata.com", "AGD-245");
        user.AddPoints(400);
        users.Add(user);
        var product = Product.CreateNew("AGDATA Gaming Voucher", 200, stock: null);
        products.Add(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        await service.CancelRedemptionAsync(CreateAdmin(), redemptionId);

        var updatedUser = await users.GetByIdAsync(user.Id);
        var redemption = await redemptions.GetByIdAsync(redemptionId);
        Assert.Equal(0, updatedUser!.LockedPoints);
        Assert.Equal(RedemptionStatus.Canceled, redemption!.Status);
    }

    [Fact]
    public async Task ApproveRedemptionAsync_WhenMissing_ShouldThrow()
    {
        var (service, _, _, _, _) = BuildService();

        await Assert.ThrowsAsync<DomainException>(() => service.ApproveRedemptionAsync(CreateAdmin(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_WhenRedemptionMissing_ShouldThrow()
    {
        var (service, _, _, _, _) = BuildService();

        await Assert.ThrowsAsync<DomainException>(() => service.DeliverRedemptionAsync(CreateAdmin(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_WhenUserMissing_ShouldThrow()
    {
        var (service, _, products, redemptions, _) = BuildService();
        var product = Product.CreateNew("AGDATA Soil Sensor", 350, stock: 1);
        products.Add(product);
        var redemption = Redemption.CreateNew(Guid.NewGuid(), product.Id);
        redemptions.Add(redemption);

        await Assert.ThrowsAsync<DomainException>(() => service.DeliverRedemptionAsync(CreateAdmin(), redemption.Id));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_WhenProductMissing_ShouldThrow()
    {
        var (service, users, _, redemptions, _) = BuildService();
        var user = User.CreateNew("Isaac Wood", "isaac.wood@agdata.com", "AGD-249");
        user.AddPoints(500);
        users.Add(user);
        var redemption = Redemption.CreateNew(user.Id, Guid.NewGuid());
        redemptions.Add(redemption);

        await Assert.ThrowsAsync<DomainException>(() => service.DeliverRedemptionAsync(CreateAdmin(), redemption.Id));
    }

    [Fact]
    public async Task RejectRedemptionAsync_WhenMissing_ShouldThrow()
    {
        var (service, _, _, _, _) = BuildService();

        await Assert.ThrowsAsync<DomainException>(() => service.RejectRedemptionAsync(CreateAdmin(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CancelRedemptionAsync_WhenMissing_ShouldThrow()
    {
        var (service, _, _, _, _) = BuildService();

        await Assert.ThrowsAsync<DomainException>(() => service.CancelRedemptionAsync(CreateAdmin(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_WhenStockInsufficient_ShouldThrow()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = User.CreateNew("Hugo Miles", "hugo.miles@agdata.com", "AGD-250");
        user.AddPoints(1000);
        users.Add(user);
        var product = Product.CreateNew("AGDATA Demo Kit", 200, stock: 0);
        products.Add(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);
        await service.ApproveRedemptionAsync(CreateAdmin(), redemptionId);

        await Assert.ThrowsAsync<DomainException>(() => service.DeliverRedemptionAsync(CreateAdmin(), redemptionId));

        var redemption = await redemptions.GetByIdAsync(redemptionId);
        Assert.Equal(RedemptionStatus.Approved, redemption!.Status);
    }
}
