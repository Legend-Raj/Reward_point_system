using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class PointsLedgerServiceTests
{
    private sealed class CapturingPointsTransactionRepository : IPointsTransactionRepository
    {
        public List<PointsTransaction> Transactions { get; } = new();
        public void Add(PointsTransaction transaction) => Transactions.Add(transaction);
    }

    private static (PointsLedgerService service, UserRepositoryInMemory users, EventRepositoryInMemory events, CapturingPointsTransactionRepository txRepo)
        BuildService()
    {
        var userRepo = new UserRepositoryInMemory();
        var eventRepo = new EventRepositoryInMemory();
        var txRepo = new CapturingPointsTransactionRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var service = new PointsLedgerService(userRepo, eventRepo, txRepo, unitOfWork);
        return (service, userRepo, eventRepo, txRepo);
    }

    private static Admin CreateAdmin() => Admin.CreateNew("Chloe Patel", "chloe.patel@agdata.com", "AGD-ADMIN-210");

    [Fact]
    public async Task AllocatePoints_ShouldCreditUserAndLogTransaction()
    {
        var (service, userRepo, eventRepo, txRepo) = BuildService();
        var user = User.CreateNew("Ravi Desai", "ravi.desai@agdata.com", "AGD-131");
        var rewardEvent = Event.CreateNew("AGDATA Agronomy Hackathon", DateTimeOffset.UtcNow);
        userRepo.Add(user);
        eventRepo.Add(rewardEvent);

        var transactionId = await service.AllocatePointsToUserForEventAsync(CreateAdmin(), user.Id, rewardEvent.Id, 100);

        var updatedUser = await userRepo.GetByIdAsync(user.Id);
        Assert.Equal(100, updatedUser!.TotalPoints);
        Assert.Single(txRepo.Transactions);
        Assert.Equal(TransactionType.Earn, txRepo.Transactions[0].Type);
        Assert.Equal(transactionId, txRepo.Transactions[0].Id);
    }

    [Fact]
    public async Task AllocatePoints_WhenUserInactive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var inactiveUser = new User(Guid.NewGuid(), "Inactive Analyst", new("inactive@agdata.com"), new("AGD-132"), isActive: false);
        var rewardEvent = Event.CreateNew("AGDATA Agronomy Hackathon", DateTimeOffset.UtcNow);
        userRepo.Add(inactiveUser);
        eventRepo.Add(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.AllocatePointsToUserForEventAsync(CreateAdmin(), inactiveUser.Id, rewardEvent.Id, 10));
    }

    [Fact]
    public async Task AllocatePoints_WhenEventInactive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var user = User.CreateNew("Anita Gomez", "anita.gomez@agdata.com", "AGD-133");
        var rewardEvent = Event.CreateNew("AGDATA Agronomy Hackathon", DateTimeOffset.UtcNow);
        rewardEvent.MakeInactive();
        userRepo.Add(user);
        eventRepo.Add(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.AllocatePointsToUserForEventAsync(CreateAdmin(), user.Id, rewardEvent.Id, 10));
    }

    [Fact]
    public async Task AllocatePoints_WhenUserMissing_ShouldThrow()
    {
        var (service, _, eventRepo, _) = BuildService();
        var rewardEvent = Event.CreateNew("AGDATA Harvest Rally", DateTimeOffset.UtcNow);
        eventRepo.Add(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.AllocatePointsToUserForEventAsync(CreateAdmin(), Guid.NewGuid(), rewardEvent.Id, 50));
    }

    [Fact]
    public async Task AllocatePoints_WhenEventMissing_ShouldThrow()
    {
        var (service, userRepo, _, _) = BuildService();
        var user = User.CreateNew("Evelyn Park", "evelyn.park@agdata.com", "AGD-134");
        userRepo.Add(user);

        await Assert.ThrowsAsync<DomainException>(() => service.AllocatePointsToUserForEventAsync(CreateAdmin(), user.Id, Guid.NewGuid(), 75));
    }

    [Fact]
    public async Task AllocatePoints_WhenPointsNonPositive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var user = User.CreateNew("Jon Rivera", "jon.rivera@agdata.com", "AGD-135");
        var rewardEvent = Event.CreateNew("AGDATA Innovation Day", DateTimeOffset.UtcNow);
        userRepo.Add(user);
        eventRepo.Add(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.AllocatePointsToUserForEventAsync(CreateAdmin(), user.Id, rewardEvent.Id, 0));
        await Assert.ThrowsAsync<DomainException>(() => service.AllocatePointsToUserForEventAsync(CreateAdmin(), user.Id, rewardEvent.Id, -10));
    }
}
