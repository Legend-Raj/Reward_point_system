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

    private static Admin CreateAdmin() => Admin.CreateNew("Teacher", "teacher@example.com", "ADMIN-10");

    [Fact]
    public async Task AllocatePoints_ShouldCreditUserAndLogTransaction()
    {
        var (service, userRepo, eventRepo, txRepo) = BuildService();
        var user = User.CreateNew("Student", "student@example.com", "EMP-31");
        var rewardEvent = Event.CreateNew("Hackathon", DateTimeOffset.UtcNow);
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
        var inactiveUser = new User(Guid.NewGuid(), "Inactive", new("inactive@example.com"), new("EMP-32"), isActive: false);
        var rewardEvent = Event.CreateNew("Hackathon", DateTimeOffset.UtcNow);
        userRepo.Add(inactiveUser);
        eventRepo.Add(rewardEvent);

    await Assert.ThrowsAsync<DomainException>(() => service.AllocatePointsToUserForEventAsync(CreateAdmin(), inactiveUser.Id, rewardEvent.Id, 10));
    }

    [Fact]
    public async Task AllocatePoints_WhenEventInactive_ShouldThrow()
    {
        var (service, userRepo, eventRepo, _) = BuildService();
        var user = User.CreateNew("Student", "student2@example.com", "EMP-33");
        var rewardEvent = Event.CreateNew("Hackathon", DateTimeOffset.UtcNow);
        rewardEvent.MakeInactive();
        userRepo.Add(user);
        eventRepo.Add(rewardEvent);

    await Assert.ThrowsAsync<DomainException>(() => service.AllocatePointsToUserForEventAsync(CreateAdmin(), user.Id, rewardEvent.Id, 10));
    }
}
