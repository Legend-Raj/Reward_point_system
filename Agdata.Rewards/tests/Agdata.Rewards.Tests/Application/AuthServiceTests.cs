using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Auth;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class AuthServiceTests
{
    private static AuthService BuildService(IAdminRegistry? registry = null, UserRepositoryInMemory? repository = null)
    {
        repository ??= new UserRepositoryInMemory();
        registry ??= new InMemoryAdminRegistry(new[] { "admin@example.com", "ADMIN-001" });
        var unitOfWork = new InMemoryUnitOfWork();
        return new AuthService(repository, registry, unitOfWork);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldCreateStandardUser_WhenNotAdmin()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository: repository);

        var user = await service.ProvisionUserAsync("Preeti", "preeti@example.com", "EMP-10");

        Assert.NotNull(user);
        Assert.False(user is Admin);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldCreateAdmin_WhenRegistryMatches()
    {
        var repository = new UserRepositoryInMemory();
        var registry = new InMemoryAdminRegistry(new[] { "vip@example.com" });
        var service = BuildService(registry, repository);

        var user = await service.ProvisionUserAsync("Vip", "vip@example.com", "EMP-11");

        Assert.IsType<Admin>(user);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldReturnExistingUser()
    {
        var repository = new UserRepositoryInMemory();
        var existing = User.CreateNew("Existing", "existing@example.com", "EMP-12");
        repository.Add(existing);
        var service = BuildService(repository: repository);

        var result = await service.ProvisionUserAsync("New", "existing@example.com", "EMP-99");

        Assert.Equal(existing.Id, result.Id);
    }
}
