using System;
using System.Threading.Tasks;
using System.Linq;
using Agdata.Rewards.Application.DTOs.Users;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Agdata.Rewards.Tests.Common;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class UserServiceTests
{
    private static UserService BuildService(UserRepositoryInMemory repository)
        => new(repository, new InMemoryUnitOfWork());

    [Fact]
    public async Task CreateNewUserAsync_ShouldPersistUser()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        var (first, middle, last) = NameTestHelper.Split("Priya Singh");
        var user = await service.CreateNewUserAsync(first, middle, last, "priya.singh@agdata.com", "AGD-321");

        Assert.Equal(user.Id, (await repository.GetUserByIdAsync(user.Id))?.Id);
        Assert.True(user.CreatedAt <= user.UpdatedAt);
        Assert.True((DateTimeOffset.UtcNow - user.CreatedAt) < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateNewUserAsync_WhenEmailExists_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        repository.AddUser(CreateUser("Existing User", "duplication@agdata.com", "AGD-322"));

        var duplicate = NameTestHelper.Split("Another User");

        await Assert.ThrowsAsync<DomainException>(() => service.CreateNewUserAsync(duplicate.First, duplicate.Middle, duplicate.Last, "duplication@agdata.com", "AGD-923"));
    }

    [Fact]
    public async Task CreateNewUserAsync_WhenEmployeeIdExists_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        repository.AddUser(CreateUser("Existing User", "existing@agdata.com", "AGD-324"));

        var another = NameTestHelper.Split("Another User");

        await Assert.ThrowsAsync<DomainException>(() => service.CreateNewUserAsync(another.First, another.Middle, another.Last, "new@agdata.com", "AGD-324"));
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        var parts = NameTestHelper.Split("Ishaan Chhabra");
        var user = await service.CreateNewUserAsync(parts.First, parts.Middle, parts.Last, "ishaan.chhabra@agdata.com", "AGD-401");

        var found = await service.GetByEmailAsync("ishaan.chhabra@agdata.com");

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldApplyChangesAndRespectUniqueness()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        var originalParts = NameTestHelper.Split("Mia Johnson");
        var otherParts = NameTestHelper.Split("Chris Park");
        var original = await service.CreateNewUserAsync(originalParts.First, originalParts.Middle, originalParts.Last, "mia.johnson@agdata.com", "AGD-500");
        var other = await service.CreateNewUserAsync(otherParts.First, otherParts.Middle, otherParts.Last, "chris.park@agdata.com", "AGD-501");

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateUserAsync(other.Id, null, null, null, "mia.johnson@agdata.com", null, null));

        var previousUpdatedAt = original.UpdatedAt;

        var updated = await service.UpdateUserAsync(original.Id, "Mia", "J.", "Johnson", "mia.j@agdata.com", "AGD-550", false);

        Assert.Equal("Mia", updated.Name.FirstName);
        Assert.Equal("J.", updated.Name.MiddleName);
        Assert.Equal("Johnson", updated.Name.LastName);
        Assert.Equal("Mia J. Johnson", updated.Name.FullName);
        Assert.Equal("mia.j@agdata.com", updated.Email.Value);
        Assert.Equal("AGD-550", updated.EmployeeId.Value);
        Assert.False(updated.IsActive);
        Assert.True(updated.UpdatedAt > previousUpdatedAt);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserMissing_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateUserAsync(Guid.NewGuid(), null, null, null, null, null, null));
    }

    [Fact]
    public async Task ActivateUserAsync_ShouldSetUserActive()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        var parts = NameTestHelper.Split("Nina Hart");
        var user = await service.CreateNewUserAsync(parts.First, parts.Middle, parts.Last, "nina.hart@agdata.com", "AGD-810");

        var previousUpdatedAt = user.UpdatedAt;

        var activated = await service.ActivateUserAsync(user.Id);

        Assert.True(activated.IsActive);
        Assert.True(activated.UpdatedAt >= previousUpdatedAt);

        var deactivated = await service.DeactivateUserAsync(user.Id);
        var reactivateUpdatedAt = deactivated.UpdatedAt;

        Assert.False(deactivated.IsActive);

        var reactivated = await service.ActivateUserAsync(user.Id);

        Assert.True(reactivated.IsActive);
        Assert.True(reactivated.UpdatedAt > reactivateUpdatedAt);
    }

    [Fact]
    public async Task ActivateUserAsync_WhenUserMissing_ShouldThrow()
    {
        var service = BuildService(new UserRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.ActivateUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeactivateUserAsync_WhenUserMissing_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);

        await Assert.ThrowsAsync<DomainException>(() => service.DeactivateUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListUsersAsync_ShouldReturnAllUsers()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);

        var firstParts = NameTestHelper.Split("Jordan Patel");
        var secondParts = NameTestHelper.Split("Leah Brown");

        var firstUser = await service.CreateNewUserAsync(firstParts.First, firstParts.Middle, firstParts.Last, "jordan.patel@agdata.com", "AGD-701");
        var secondUser = await service.CreateNewUserAsync(secondParts.First, secondParts.Middle, secondParts.Last, "leah.brown@agdata.com", "AGD-702");

        var users = await service.ListUsersAsync();

        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Id == firstUser.Id);
        Assert.Contains(users, u => u.Id == secondUser.Id);
    }

    [Fact]
    public async Task QueryUsersAsync_ShouldFilterByStatusSearchAndPaginate()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);

        var activeParts = NameTestHelper.Split("Jordan Patel");
        var inactiveParts = NameTestHelper.Split("Leah Brown");
        var thirdParts = NameTestHelper.Split("Priya Nair");

        var active = await service.CreateNewUserAsync(activeParts.First, activeParts.Middle, activeParts.Last, "jordan.patel@agdata.com", "AGD-701");
        var inactive = await service.CreateNewUserAsync(inactiveParts.First, inactiveParts.Middle, inactiveParts.Last, "leah.brown@agdata.com", "AGD-702");
        await service.UpdateUserAsync(inactive.Id, null, null, null, null, null, false);
        await service.CreateNewUserAsync(thirdParts.First, thirdParts.Middle, thirdParts.Last, "priya.nair@agdata.com", "AGD-703");

        var result = await service.QueryUsersAsync(new UserQueryOptions(Skip: 0, Take: 1, IsActive: true, Search: "Patel"));

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(active.Id, result.Items[0].Id);
        Assert.Equal(0, result.Skip);
        Assert.Equal(1, result.Take);

        var inactiveResult = await service.QueryUsersAsync(new UserQueryOptions(Skip: 0, Take: 5, IsActive: false));

        Assert.Single(inactiveResult.Items);
        Assert.Equal(inactive.Id, inactiveResult.Items[0].Id);
    }

    [Fact]
    public async Task QueryUsersAsync_WhenValidationFails_ShouldThrow()
    {
        var service = BuildService(new UserRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.QueryUsersAsync(new UserQueryOptions(Skip: -1, Take: 10)));
        await Assert.ThrowsAsync<DomainException>(() => service.QueryUsersAsync(new UserQueryOptions(Skip: 0, Take: 0)));
        await Assert.ThrowsAsync<DomainException>(() => service.QueryUsersAsync(new UserQueryOptions(Skip: 0, Take: 101)));
    }

    private static User CreateUser(string fullName, string email, string employeeId)
    {
        var (first, middle, last) = NameTestHelper.Split(fullName);
        return User.CreateNew(first, middle, last, email, employeeId);
    }
}
