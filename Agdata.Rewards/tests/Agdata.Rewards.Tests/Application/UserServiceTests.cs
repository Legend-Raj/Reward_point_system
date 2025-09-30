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
    public async Task CreateNewUserAsync_WithInvalidEmail_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository);

        await Assert.ThrowsAsync<DomainException>(() => service.CreateNewUserAsync("Invalid", "not-an-email", "AGD-400"));
    }
}
