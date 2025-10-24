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

public class PointsLedgerServiceTests
{
    private sealed class CapturingLedgerEntryRepository : ILedgerEntryRepository
    {
        public List<LedgerEntry> Entries { get; } = new();

        public void AddLedgerEntry(LedgerEntry entry) => Entries.Add(entry);

        public Task<IReadOnlyList<LedgerEntry>> ListLedgerEntriesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var slice = Entries
                .Where(tx => tx.UserId == userId)
                .OrderBy(tx => tx.Timestamp)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<LedgerEntry>>(slice);
        }
    }

    private static (PointsLedgerService service, UserRepositoryInMemory users, EventRepositoryInMemory events, CapturingLedgerEntryRepository ledgerRepo)
        BuildService()
    {
        var userRepo = new UserRepositoryInMemory();
        var eventRepo = new EventRepositoryInMemory();
        var ledgerRepo = new CapturingLedgerEntryRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var pointsService = new PointsService(userRepo, unitOfWork);
        var service = new PointsLedgerService(userRepo, eventRepo, ledgerRepo, pointsService, unitOfWork);
        return (service, userRepo, eventRepo, ledgerRepo);
    }

    private static Admin CreateTestAdmin()
    {
        var (first, middle, last) = NameTestHelper.Split("Test Admin");
        return Admin.CreateNew(first, middle, last, "admin@agdata.com", "AGD-001");
    }

    private static User CreateUser(string fullName, string email, string employeeId)
    {
        var (first, middle, last) = NameTestHelper.Split(fullName);
        return User.CreateNew(first, middle, last, email, employeeId);
    }

    private static Admin CreateInactiveAdmin(string fullName, string email, string employeeId)
    {
        var (first, middle, last) = NameTestHelper.Split(fullName);
        return new Admin(Guid.NewGuid(), PersonName.Create(first, middle, last), new Email(email), new EmployeeId(employeeId), isActive: false);
    }

    [Fact]
    public async Task EarnAsync_ShouldCreditUserAndLogTransaction()
    {
        var (service, userRepo, eventRepo, ledgerRepo) = BuildService();
        var admin = CreateTestAdmin();
        var user = CreateUser("Ravi Desai", "ravi.desai@agdata.com", "AGD-131");
        var rewardEvent = Event.CreateNew("AGDATA Agronomy Hackathon", DateTimeOffset.UtcNow);
    userRepo.AddUser(user);
    eventRepo.AddEvent(rewardEvent);

        var entry = await service.EarnAsync(admin, user.Id, rewardEvent.Id, 100);

    var updatedUser = await userRepo.GetUserByIdAsync(user.Id);
        Assert.Equal(100, updatedUser!.TotalPoints);
        Assert.Single(ledgerRepo.Entries);
        Assert.Equal(LedgerEntryType.Earn, ledgerRepo.Entries[0].Type);
        Assert.Equal(entry.Id, ledgerRepo.Entries[0].Id);
    }

    [Fact]
    public async Task EarnAsync_WhenUserInactive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var admin = CreateTestAdmin();
        var inactiveUser = new User(Guid.NewGuid(), PersonName.Create("Inactive", null, "Analyst"), new Email("inactive@agdata.com"), new EmployeeId("AGD-132"), isActive: false);
        var rewardEvent = Event.CreateNew("AGDATA Agronomy Hackathon", DateTimeOffset.UtcNow);
    userRepo.AddUser(inactiveUser);
    eventRepo.AddEvent(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(admin, inactiveUser.Id, rewardEvent.Id, 10));
    }

    [Fact]
    public async Task EarnAsync_WhenEventInactive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var admin = CreateTestAdmin();
        var user = CreateUser("Anita Gomez", "anita.gomez@agdata.com", "AGD-133");
        var rewardEvent = Event.CreateNew("AGDATA Agronomy Hackathon", DateTimeOffset.UtcNow);
        rewardEvent.MakeInactive();
    userRepo.AddUser(user);
    eventRepo.AddEvent(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(admin, user.Id, rewardEvent.Id, 10));
    }

    [Fact]
    public async Task EarnAsync_WhenUserMissing_ShouldThrow()
    {
        var (service, _, eventRepo, _) = BuildService();
        var admin = CreateTestAdmin();
    var rewardEvent = Event.CreateNew("AGDATA Harvest Rally", DateTimeOffset.UtcNow);
    eventRepo.AddEvent(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(admin, Guid.NewGuid(), rewardEvent.Id, 50));
    }

    [Fact]
    public async Task EarnAsync_WhenEventMissing_ShouldThrow()
    {
        var (service, userRepo, _, _) = BuildService();
        var admin = CreateTestAdmin();
    var user = CreateUser("Evelyn Park", "evelyn.park@agdata.com", "AGD-134");
    userRepo.AddUser(user);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(admin, user.Id, Guid.NewGuid(), 75));
    }

    [Fact]
    public async Task EarnAsync_WhenPointsNonPositive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var admin = CreateTestAdmin();
        var user = CreateUser("Jon Rivera", "jon.rivera@agdata.com", "AGD-135");
        var rewardEvent = Event.CreateNew("AGDATA Innovation Day", DateTimeOffset.UtcNow);
    userRepo.AddUser(user);
    eventRepo.AddEvent(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(admin, user.Id, rewardEvent.Id, 0));
        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(admin, user.Id, rewardEvent.Id, -10));
    }

    [Fact]
    public async Task GetUserTransactionHistoryAsync_ShouldReturnChronologicalTransactions()
    {
        var (service, userRepo, eventRepo, ledgerRepo) = BuildService();
        var user = CreateUser("Sana Iyer", "sana.iyer@agdata.com", "AGD-136");
    var rewardEvent = Event.CreateNew("AGDATA Soil Summit", DateTimeOffset.UtcNow);
    userRepo.AddUser(user);
    eventRepo.AddEvent(rewardEvent);

        ledgerRepo.AddLedgerEntry(new LedgerEntry(
            Guid.NewGuid(),
            user.Id,
            LedgerEntryType.Earn,
            40,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            eventId: rewardEvent.Id));
        ledgerRepo.AddLedgerEntry(new LedgerEntry(
            Guid.NewGuid(),
            user.Id,
            LedgerEntryType.Earn,
            20,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            eventId: rewardEvent.Id));

        var page = await service.GetUserTransactionHistoryAsync(user.Id, skip: 0, take: 10);

        Assert.Equal(2, page.Items.Count);
        Assert.True(page.Items[0].Timestamp >= page.Items[1].Timestamp);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(0, page.Skip);
        Assert.Equal(10, page.Take);

        var secondPage = await service.GetUserTransactionHistoryAsync(user.Id, skip: 1, take: 1);
        Assert.Single(secondPage.Items);
        Assert.Equal(page.Items[1].Id, secondPage.Items[0].Id);
    }

    [Fact]
    public async Task EarnAsync_ShouldRejectMissingAdmin()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
    var user = CreateUser("Priya Rao", "priya.rao@agdata.com", "AGD-140");
    var rewardEvent = Event.CreateNew("AGDATA Innovation Forum", DateTimeOffset.UtcNow);
    userRepo.AddUser(user);
    eventRepo.AddEvent(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(null!, user.Id, rewardEvent.Id, 25));
    }

    [Fact]
    public async Task EarnAsync_ShouldRejectInactiveAdmin()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
    var inactiveAdmin = CreateInactiveAdmin("Suspended Admin", "suspended@agdata.com", "AGD-999");
    var user = CreateUser("Rahul Jain", "rahul.jain@agdata.com", "AGD-141");
    var rewardEvent = Event.CreateNew("AGDATA Field Jam", DateTimeOffset.UtcNow);
    userRepo.AddUser(user);
    eventRepo.AddEvent(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(inactiveAdmin, user.Id, rewardEvent.Id, 30));
    }

    [Fact]
    public async Task GetUserTransactionHistoryAsync_WhenUserMissing_ShouldThrow()
    {
        var (service, _, _, _) = BuildService();

        await Assert.ThrowsAsync<DomainException>(() => service.GetUserTransactionHistoryAsync(Guid.NewGuid(), 0, 10));
    }

    [Fact]
    public async Task GetUserTransactionHistoryAsync_WhenPagingParametersInvalid_ShouldThrow()
    {
        var (service, userRepo, eventRepo, ledgerRepo) = BuildService();
        var user = CreateUser("Paging User", "paging.user@agdata.com", "AGD-150");
        var evt = Event.CreateNew("Paging Event", DateTimeOffset.UtcNow);
        userRepo.AddUser(user);
        eventRepo.AddEvent(evt);
        ledgerRepo.AddLedgerEntry(new LedgerEntry(
            Guid.NewGuid(),
            user.Id,
            LedgerEntryType.Earn,
            10,
            DateTimeOffset.UtcNow,
            eventId: evt.Id));

        await Assert.ThrowsAsync<DomainException>(() => service.GetUserTransactionHistoryAsync(user.Id, -1, 10));
        await Assert.ThrowsAsync<DomainException>(() => service.GetUserTransactionHistoryAsync(user.Id, 0, 0));
        await Assert.ThrowsAsync<DomainException>(() => service.GetUserTransactionHistoryAsync(user.Id, 0, 101));
    }

}
