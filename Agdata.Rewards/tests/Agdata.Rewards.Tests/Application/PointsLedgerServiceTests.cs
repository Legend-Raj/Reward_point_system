using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public Task<IReadOnlyList<PointsTransaction>> GetByUserIdAsync(Guid userId)
        {
            var slice = Transactions
                .Where(tx => tx.UserId == userId)
                .OrderBy(tx => tx.Timestamp)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<PointsTransaction>>(slice);
        }
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

    [Fact]
    public async Task EarnAsync_ShouldCreditUserAndLogTransaction()
    {
        var (service, userRepo, eventRepo, txRepo) = BuildService();
        var user = User.CreateNew("Ravi Desai", "ravi.desai@agdata.com", "AGD-131");
        var rewardEvent = Event.CreateNew("AGDATA Agronomy Hackathon", DateTimeOffset.UtcNow);
        userRepo.Add(user);
        eventRepo.Add(rewardEvent);

        var transaction = await service.EarnAsync(user.Id, rewardEvent.Id, 100);

        var updatedUser = await userRepo.GetByIdAsync(user.Id);
        Assert.Equal(100, updatedUser!.TotalPoints);
        Assert.Single(txRepo.Transactions);
        Assert.Equal(TransactionType.Earn, txRepo.Transactions[0].Type);
        Assert.Equal(transaction.Id, txRepo.Transactions[0].Id);
    }

    [Fact]
    public async Task EarnAsync_WhenUserInactive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var inactiveUser = new User(Guid.NewGuid(), "Inactive Analyst", new("inactive@agdata.com"), new("AGD-132"), isActive: false);
        var rewardEvent = Event.CreateNew("AGDATA Agronomy Hackathon", DateTimeOffset.UtcNow);
        userRepo.Add(inactiveUser);
        eventRepo.Add(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(inactiveUser.Id, rewardEvent.Id, 10));
    }

    [Fact]
    public async Task EarnAsync_WhenEventInactive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var user = User.CreateNew("Anita Gomez", "anita.gomez@agdata.com", "AGD-133");
        var rewardEvent = Event.CreateNew("AGDATA Agronomy Hackathon", DateTimeOffset.UtcNow);
        rewardEvent.MakeInactive();
        userRepo.Add(user);
        eventRepo.Add(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(user.Id, rewardEvent.Id, 10));
    }

    [Fact]
    public async Task EarnAsync_WhenUserMissing_ShouldThrow()
    {
        var (service, _, eventRepo, _) = BuildService();
        var rewardEvent = Event.CreateNew("AGDATA Harvest Rally", DateTimeOffset.UtcNow);
        eventRepo.Add(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(Guid.NewGuid(), rewardEvent.Id, 50));
    }

    [Fact]
    public async Task EarnAsync_WhenEventMissing_ShouldThrow()
    {
        var (service, userRepo, _, _) = BuildService();
        var user = User.CreateNew("Evelyn Park", "evelyn.park@agdata.com", "AGD-134");
        userRepo.Add(user);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(user.Id, Guid.NewGuid(), 75));
    }

    [Fact]
    public async Task EarnAsync_WhenPointsNonPositive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var user = User.CreateNew("Jon Rivera", "jon.rivera@agdata.com", "AGD-135");
        var rewardEvent = Event.CreateNew("AGDATA Innovation Day", DateTimeOffset.UtcNow);
        userRepo.Add(user);
        eventRepo.Add(rewardEvent);

        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(user.Id, rewardEvent.Id, 0));
        await Assert.ThrowsAsync<DomainException>(() => service.EarnAsync(user.Id, rewardEvent.Id, -10));
    }

    [Fact]
    public async Task GetUserTransactionHistoryAsync_ShouldReturnChronologicalTransactions()
    {
        var (service, userRepo, eventRepo, txRepo) = BuildService();
        var user = User.CreateNew("Sana Iyer", "sana.iyer@agdata.com", "AGD-136");
        var rewardEvent = Event.CreateNew("AGDATA Soil Summit", DateTimeOffset.UtcNow);
        userRepo.Add(user);
        eventRepo.Add(rewardEvent);

        txRepo.Add(new PointsTransaction(Guid.NewGuid(), user.Id, TransactionType.Earn, 40, DateTimeOffset.UtcNow.AddMinutes(-5), rewardEvent.Id));
        txRepo.Add(new PointsTransaction(Guid.NewGuid(), user.Id, TransactionType.Earn, 20, DateTimeOffset.UtcNow.AddMinutes(-1), rewardEvent.Id));

        var history = await service.GetUserTransactionHistoryAsync(user.Id);

        Assert.Equal(2, history.Count);
        Assert.True(history[0].Timestamp <= history[1].Timestamp);
    }

    [Fact]
    public async Task EventManagement_ShouldCreateListAndToggle()
    {
        var (service, _, _, _) = BuildService();

        var evt = await service.CreateEventAsync("AGDATA Data Dive", true);
        await service.SetEventActiveAsync(evt.Id, false);

        var events = await service.ListEventsAsync(onlyActive: false);

        Assert.Single(events);
        Assert.False(events[0].IsActive);
    }
}
