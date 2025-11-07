using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Agdata.Rewards.Tests.Common;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class RedemptionRequestServiceTests
{
    private sealed class CapturingLedgerEntryRepository : ILedgerEntryRepository
    {
        public List<LedgerEntry> Entries { get; } = new();

        public void AddLedgerEntry(LedgerEntry entry) => Entries.Add(entry);

        public Task<IReadOnlyList<LedgerEntry>> ListLedgerEntriesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var results = Entries
                .Where(tx => tx.UserId == userId)
                .OrderByDescending(tx => tx.Timestamp)
                .ThenBy(tx => tx.Id)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<LedgerEntry>>(results);
        }

        public Task<IReadOnlyList<LedgerEntry>> ListLedgerEntriesByUserAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
        {
            var results = Entries
                .Where(tx => tx.UserId == userId)
                .OrderByDescending(tx => tx.Timestamp)
                .ThenBy(tx => tx.Id)
                .Skip(skip)
                .Take(take)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<LedgerEntry>>(results);
        }

        public Task<int> CountLedgerEntriesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var count = Entries.Count(tx => tx.UserId == userId);
            return Task.FromResult(count);
        }
    }

    private static (RedemptionRequestService service, UserRepositoryInMemory users, ProductRepositoryInMemory products, RedemptionRequestRepositoryInMemory redemptions, CapturingLedgerEntryRepository ledgerRepo)
        BuildService()
    {
        var userRepo = new UserRepositoryInMemory();
        var productRepo = new ProductRepositoryInMemory();
        var redemptionRepo = new RedemptionRequestRepositoryInMemory();
        var ledgerRepo = new CapturingLedgerEntryRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var pointsService = new PointsService(userRepo, unitOfWork);
        var service = new RedemptionRequestService(userRepo, productRepo, redemptionRepo, ledgerRepo, pointsService, unitOfWork);
        return (service, userRepo, productRepo, redemptionRepo, ledgerRepo);
    }

    private static Admin CreateAdmin(string fullName = "Victor Alvarez", string email = "victor.alvarez@agdata.com", string employeeId = "AGD-320", bool isActive = true)
    {
        var (first, middle, last) = NameTestHelper.Split(fullName);
        return new Admin(Guid.NewGuid(), PersonName.Create(first, middle, last), new Email(email), new EmployeeId(employeeId), isActive);
    }

    private static User CreateUser(string fullName, string email, string employeeId, int totalPoints = 0)
    {
        var (first, middle, last) = NameTestHelper.Split(fullName);
        var user = User.CreateNew(first, middle, last, email, employeeId);
        // Create a new user with the specified points
        return new User(user.Id, user.Name, user.Email, user.EmployeeId, user.IsActive, totalPoints, 0, user.CreatedAt, user.UpdatedAt);
    }

    private static User CreateInactiveUser(string fullName, string email, string employeeId, int totalPoints)
    {
        var (first, middle, last) = NameTestHelper.Split(fullName);
        return new User(Guid.NewGuid(), PersonName.Create(first, middle, last), new Email(email), new EmployeeId(employeeId), isActive: false, totalPoints: totalPoints);
    }

    [Fact]
    public async Task RequestRedemptionAsync_ShouldReservePointsAndCreatePending()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = CreateUser("Jada Holmes", "jada.holmes@agdata.com", "AGD-241", 1000);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Scout Tablet", 500, stock: 5);
        products.AddProduct(product);

        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        var storedRedemption = await redemptions.GetRedemptionRequestByIdAsync(redemptionId);
        Assert.NotNull(storedRedemption);
        Assert.Equal(RedemptionRequestStatus.Pending, storedRedemption!.Status);
        Assert.Equal(500, (await users.GetUserByIdAsync(user.Id))!.LockedPoints);
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenDuplicatePending_ShouldThrow()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = CreateUser("Noah Kim", "noah.kim@agdata.com", "AGD-242", 1000);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Field Headset", 400, stock: 5);
        products.AddProduct(product);
        redemptions.AddRedemptionRequest(RedemptionRequest.CreateNew(user.Id, product.Id));

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenUserInactive_ShouldThrow()
    {
        var (service, users, products, _, _) = BuildService();
        var user = CreateInactiveUser("Inactive Agronomist", "inactive@agdata.com", "AGD-246", totalPoints: 500);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Lamp", 200, stock: 1);
        products.AddProduct(product);

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenProductInactive_ShouldThrow()
    {
        var (service, users, products, _, _) = BuildService();
        var user = CreateUser("Zuri Carson", "zuri.carson@agdata.com", "AGD-247", 500);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Precision Mouse", 150, stock: 1);
        product.MakeInactive();
        products.AddProduct(product);

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task RequestRedemptionAsync_WhenInsufficientPoints_ShouldThrow()
    {
        var (service, users, products, _, _) = BuildService();
        var user = CreateUser("Reese Patel", "reese.patel@agdata.com", "AGD-248", 100);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Drone Kit", 600, stock: 2);
        products.AddProduct(product);

        await Assert.ThrowsAsync<DomainException>(() => service.RequestRedemptionAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_ShouldCapturePointsReduceStockAndLog()
    {
        var (service, users, products, redemptions, ledgerRepo) = BuildService();
        var user = CreateUser("Elena Brooks", "elena.brooks@agdata.com", "AGD-243", 1000);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Smart Watch", 300, stock: 2);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);
        await service.ApproveRedemptionAsync(CreateAdmin(), redemptionId);

        await service.DeliverRedemptionAsync(CreateAdmin(), redemptionId);

        var updatedUser = await users.GetUserByIdAsync(user.Id);
        var updatedProduct = await products.GetProductByIdAsync(product.Id);
        var updatedRedemption = await redemptions.GetRedemptionRequestByIdAsync(redemptionId);

        Assert.Equal(700, updatedUser!.TotalPoints);
        Assert.Equal(0, updatedUser.LockedPoints);
        Assert.Equal(1, updatedProduct!.Stock);
        Assert.Equal(RedemptionRequestStatus.Delivered, updatedRedemption!.Status);
        Assert.Single(ledgerRepo.Entries);
        Assert.Equal(LedgerEntryType.Redeem, ledgerRepo.Entries[0].Type);
    }

    [Fact]
    public async Task DeliverRedemptionAsync_WhenStockUnlimited_ShouldLeaveStockUnchanged()
    {
        var (service, users, products, redemptions, ledgerRepo) = BuildService();
        var user = CreateUser("Adrian Blake", "adrian.blake@agdata.com", "AGD-260", 800);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Executive Membership", 500, stock: null);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);
        await service.ApproveRedemptionAsync(CreateAdmin(), redemptionId);

        await service.DeliverRedemptionAsync(CreateAdmin(), redemptionId);

        var persistedProduct = await products.GetProductByIdAsync(product.Id);
        Assert.Null(persistedProduct!.Stock);
        Assert.Single(ledgerRepo.Entries);
        Assert.Equal(LedgerEntryType.Redeem, ledgerRepo.Entries[0].Type);
    }

    [Fact]
    public async Task RejectRedemptionAsync_ShouldReleaseReservedPoints()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = CreateUser("Marcus Lane", "marcus.lane@agdata.com", "AGD-244", 600);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Gift Card", 300, stock: null);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        await service.RejectRedemptionAsync(CreateAdmin(), redemptionId);

        var updatedUser = await users.GetUserByIdAsync(user.Id);
        var redemption = await redemptions.GetRedemptionRequestByIdAsync(redemptionId);
        Assert.Equal(0, updatedUser!.LockedPoints);
        Assert.Equal(RedemptionRequestStatus.Rejected, redemption!.Status);
    }

    [Fact]
    public async Task CancelRedemptionAsync_ShouldReleaseReservedPoints()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = CreateUser("Sofia Quinn", "sofia.quinn@agdata.com", "AGD-245", 400);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Gaming Voucher", 200, stock: null);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        await service.CancelRedemptionAsync(CreateAdmin(), redemptionId);

        var updatedUser = await users.GetUserByIdAsync(user.Id);
        var redemption = await redemptions.GetRedemptionRequestByIdAsync(redemptionId);
        Assert.Equal(0, updatedUser!.LockedPoints);
        Assert.Equal(RedemptionRequestStatus.Canceled, redemption!.Status);
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
        products.AddProduct(product);
        var redemption = RedemptionRequest.CreateNew(Guid.NewGuid(), product.Id);
        redemptions.AddRedemptionRequest(redemption);

        await Assert.ThrowsAsync<DomainException>(() => service.DeliverRedemptionAsync(CreateAdmin(), redemption.Id));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_WhenProductMissing_ShouldThrow()
    {
        var (service, users, _, redemptions, _) = BuildService();
        var user = CreateUser("Isaac Wood", "isaac.wood@agdata.com", "AGD-249", 500);
        users.AddUser(user);
        var redemption = RedemptionRequest.CreateNew(user.Id, Guid.NewGuid());
        redemptions.AddRedemptionRequest(redemption);

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
        var user = CreateUser("Hugo Miles", "hugo.miles@agdata.com", "AGD-250", 1000);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Demo Kit", 200, stock: 0);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);
        await service.ApproveRedemptionAsync(CreateAdmin(), redemptionId);

        await Assert.ThrowsAsync<DomainException>(() => service.DeliverRedemptionAsync(CreateAdmin(), redemptionId));

        var redemption = await redemptions.GetRedemptionRequestByIdAsync(redemptionId);
        Assert.Equal(RedemptionRequestStatus.Approved, redemption!.Status);
    }

    [Fact]
    public async Task ApproveRedemptionAsync_ShouldTransitionToApproved()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = CreateUser("Lina Chen", "lina.chen@agdata.com", "AGD-261", 600);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Softshell", 300, stock: 2);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        await service.ApproveRedemptionAsync(CreateAdmin(), redemptionId);

        var redemption = await redemptions.GetRedemptionRequestByIdAsync(redemptionId);
        Assert.Equal(RedemptionRequestStatus.Approved, redemption!.Status);
        Assert.NotNull(redemption.ApprovedAt);
    }

    [Fact]
    public async Task ApproveRedemptionAsync_WhenNotPending_ShouldThrow()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var user = CreateUser("Kara Singh", "kara.singh@agdata.com", "AGD-262", 800);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Field Boots", 400, stock: 2);
        products.AddProduct(product);
        var redemption = RedemptionRequest.CreateNew(user.Id, product.Id);
        redemption.Approve();
        redemptions.AddRedemptionRequest(redemption);

        await Assert.ThrowsAsync<DomainException>(() => service.ApproveRedemptionAsync(CreateAdmin(), redemption.Id));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_WhenNotApproved_ShouldThrowAndPreserveState()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var admin = CreateAdmin();
        var user = CreateUser("Diego Torres", "diego.torres@agdata.com", "AGD-263", 900);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Tech Pack", 450, stock: 2);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        await Assert.ThrowsAsync<DomainException>(() => service.DeliverRedemptionAsync(admin, redemptionId));

        var storedRedemption = await redemptions.GetRedemptionRequestByIdAsync(redemptionId);
        var storedUser = await users.GetUserByIdAsync(user.Id);
        var storedProduct = await products.GetProductByIdAsync(product.Id);

        Assert.Equal(RedemptionRequestStatus.Pending, storedRedemption!.Status);
        Assert.Equal(450, storedUser!.LockedPoints);
        Assert.Equal(2, storedProduct!.Stock);
    }

    [Fact]
    public async Task RejectRedemptionAsync_WhenNotPending_ShouldThrowAndKeepPointsReserved()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var admin = CreateAdmin();
        var user = CreateUser("Ibrahim Malik", "ibrahim.malik@agdata.com", "AGD-264", 700);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Workshop Pass", 350, stock: 1);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);
        await service.ApproveRedemptionAsync(admin, redemptionId);

        await Assert.ThrowsAsync<DomainException>(() => service.RejectRedemptionAsync(admin, redemptionId));

        var storedUser = await users.GetUserByIdAsync(user.Id);
        Assert.Equal(350, storedUser!.LockedPoints);
    }

    [Fact]
    public async Task CancelRedemptionAsync_WhenNotPending_ShouldThrowAndKeepPointsReserved()
    {
        var (service, users, products, redemptions, _) = BuildService();
        var admin = CreateAdmin();
        var user = CreateUser("Naomi Barrett", "naomi.barrett@agdata.com", "AGD-265", 720);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Study Bundle", 360, stock: 1);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);
        await service.ApproveRedemptionAsync(admin, redemptionId);

        await Assert.ThrowsAsync<DomainException>(() => service.CancelRedemptionAsync(admin, redemptionId));

        var storedUser = await users.GetUserByIdAsync(user.Id);
        Assert.Equal(360, storedUser!.LockedPoints);
    }

    [Fact]
    public async Task ApproveRedemptionAsync_ShouldRejectMissingAdmin()
    {
        var (service, users, products, _, _) = BuildService();
        var user = CreateUser("Olivia Trent", "olivia.trent@agdata.com", "AGD-266", 500);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Notebook", 200, stock: 1);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        await Assert.ThrowsAsync<DomainException>(() => service.ApproveRedemptionAsync(null!, redemptionId));
    }

    [Fact]
    public async Task ApproveRedemptionAsync_ShouldRejectInactiveAdmin()
    {
        var (service, users, products, _, _) = BuildService();
        var user = CreateUser("Sahil Khan", "sahil.khan@agdata.com", "AGD-267", 600);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Hoodie", 250, stock: 1);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);
        var inactiveAdmin = CreateAdmin("Dormant Admin", "dormant.admin@agdata.com", "AGD-201", isActive: false);

        await Assert.ThrowsAsync<DomainException>(() => service.ApproveRedemptionAsync(inactiveAdmin, redemptionId));
    }

    [Fact]
    public async Task DeliverRedemptionAsync_ShouldRejectInvalidAdmin()
    {
        var (service, users, products, _, _) = BuildService();
        var admin = CreateAdmin();
        var user = CreateUser("Mira Das", "mira.das@agdata.com", "AGD-268", 650);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Jacket", 300, stock: 1);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);
        await service.ApproveRedemptionAsync(admin, redemptionId);
        var inactiveAdmin = CreateAdmin("Dormant Supervisor", "dormant@agdata.com", "AGD-202", isActive: false);

        await Assert.ThrowsAsync<DomainException>(() => service.DeliverRedemptionAsync(null!, redemptionId));
        await Assert.ThrowsAsync<DomainException>(() => service.DeliverRedemptionAsync(inactiveAdmin, redemptionId));
    }

    [Fact]
    public async Task RejectRedemptionAsync_ShouldRejectInvalidAdmin()
    {
        var (service, users, products, _, _) = BuildService();
        var admin = CreateAdmin();
        var user = CreateUser("Gina Howard", "gina.howard@agdata.com", "AGD-269", 520);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Pen Set", 150, stock: 1);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        await Assert.ThrowsAsync<DomainException>(() => service.RejectRedemptionAsync(null!, redemptionId));
        var inactiveAdmin = CreateAdmin("Inactive Leader", "inactive@agdata.com", "AGD-203", isActive: false);
        await Assert.ThrowsAsync<DomainException>(() => service.RejectRedemptionAsync(inactiveAdmin, redemptionId));
    }

    [Fact]
    public async Task CancelRedemptionAsync_ShouldRejectInvalidAdmin()
    {
        var (service, users, products, _, _) = BuildService();
        var user = CreateUser("Henry Cole", "henry.cole@agdata.com", "AGD-270", 480);
        users.AddUser(user);
        var product = Product.CreateNew("AGDATA Field Gloves", 200, stock: 1);
        products.AddProduct(product);
        var redemptionId = await service.RequestRedemptionAsync(user.Id, product.Id);

        await Assert.ThrowsAsync<DomainException>(() => service.CancelRedemptionAsync(null!, redemptionId));
        var inactiveAdmin = CreateAdmin("Dormant Manager", "dormant.cancel@agdata.com", "AGD-204", isActive: false);
        await Assert.ThrowsAsync<DomainException>(() => service.CancelRedemptionAsync(inactiveAdmin, redemptionId));
    }
}
