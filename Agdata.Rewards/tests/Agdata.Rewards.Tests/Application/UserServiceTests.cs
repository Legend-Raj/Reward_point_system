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

        var user = await service.CreateNewUserAsync("Nina", "nina@example.com", "EMP-21");

        Assert.Equal(user.Id, (await repository.GetByIdAsync(user.Id))?.Id);
    }

    [Fact]
    public async Task CreateNewUserAsync_WhenEmailExists_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        repository.Add(User.CreateNew("Existing", "dup@example.com", "EMP-22"));

        await Assert.ThrowsAsync<DomainException>(() => service.CreateNewUserAsync("Another", "dup@example.com", "EMP-23"));
    }

    [Fact]
    public async Task CreateNewUserAsync_WhenEmployeeIdExists_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);
        repository.Add(User.CreateNew("Existing", "existing@example.com", "EMP-24"));

        await Assert.ThrowsAsync<DomainException>(() => service.CreateNewUserAsync("Another", "new@example.com", "EMP-24"));
    }
}
