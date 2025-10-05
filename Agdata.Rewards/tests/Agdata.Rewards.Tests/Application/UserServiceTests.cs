using System;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
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

        var user = await service.CreateNewUserAsync("Priya Singh", "priya.singh@agdata.com", "AGD-321");

        Assert.Equal(user.Id, (await repository.GetByIdAsync(user.Id))?.Id);
    }

    [Fact]
    public async Task CreateNewUserAsync_WhenEmailExists_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        repository.Add(User.CreateNew("Existing", "duplication@agdata.com", "AGD-322"));

        await Assert.ThrowsAsync<DomainException>(() => service.CreateNewUserAsync("Another", "duplication@agdata.com", "AGD-923"));
    }

    [Fact]
    public async Task CreateNewUserAsync_WhenEmployeeIdExists_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        repository.Add(User.CreateNew("Existing", "existing@agdata.com", "AGD-324"));

        await Assert.ThrowsAsync<DomainException>(() => service.CreateNewUserAsync("Another", "new@agdata.com", "AGD-324"));
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        var user = await service.CreateNewUserAsync("Ishaan Chhabra", "ishaan.chhabra@agdata.com", "AGD-401");

        var found = await service.GetByEmailAsync("ishaan.chhabra@agdata.com");

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldApplyChangesAndRespectUniqueness()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        var original = await service.CreateNewUserAsync("Mia Johnson", "mia.johnson@agdata.com", "AGD-500");
        var other = await service.CreateNewUserAsync("Chris Park", "chris.park@agdata.com", "AGD-501");

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateUserAsync(other.Id, null, "mia.johnson@agdata.com", null, null));

        var updated = await service.UpdateUserAsync(original.Id, "Mia J.", "mia.j@agdata.com", "AGD-550", false);

        Assert.Equal("Mia J.", updated.Name);
        Assert.Equal("mia.j@agdata.com", updated.Email.Value);
        Assert.Equal("AGD-550", updated.EmployeeId.Value);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserMissing_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateUserAsync(Guid.NewGuid(), null, null, null, null));
    }
}
