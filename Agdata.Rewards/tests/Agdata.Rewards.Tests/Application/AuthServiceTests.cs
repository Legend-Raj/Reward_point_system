using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
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
        registry ??= new InMemoryAdminRegistry(new[] { "safety.admin@agdata.com", "AGD-ADMIN-001" });
        var unitOfWork = new InMemoryUnitOfWork();
        return new AuthService(repository, registry, unitOfWork);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldCreateStandardUser_WhenNotAdmin()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository: repository);

        var user = await service.ProvisionUserAsync("Riya Kapoor", "riya.kapoor@agdata.com", "AGD-010");

        Assert.NotNull(user);
        Assert.False(user is Admin);
        var retrieved = await repository.GetByEmailAsync(new Email("riya.kapoor@agdata.com"));
        Assert.Equal(user.Id, retrieved!.Id);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldCreateAdmin_WhenRegistryMatchesEmail()
    {
        var repository = new UserRepositoryInMemory();
        var registry = new InMemoryAdminRegistry(new[] { "ops.director@agdata.com" });
        var service = BuildService(registry, repository);

        var user = await service.ProvisionUserAsync("Samuel Ortiz", "ops.director@agdata.com", "AGD-ADMIN-010");

        Assert.IsType<Admin>(user);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldCreateAdmin_WhenRegistryMatchesEmployeeId()
    {
        var repository = new UserRepositoryInMemory();
        var registry = new InMemoryAdminRegistry(new[] { "AGD-ADMIN-015" });
        var service = BuildService(registry, repository);

        var user = await service.ProvisionUserAsync("Morgan Lee", "morgan.lee@agdata.com", "AGD-ADMIN-015");

        Assert.IsType<Admin>(user);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldReturnExistingUser()
    {
        var repository = new UserRepositoryInMemory();
        var existing = User.CreateNew("Taylor Shah", "taylor.shah@agdata.com", "AGD-020");
        repository.Add(existing);
        var service = BuildService(repository: repository);

        var result = await service.ProvisionUserAsync("New Hire", "taylor.shah@agdata.com", "AGD-999");

        Assert.Equal(existing.Id, result.Id);
        var allReference = await repository.GetByEmailAsync(new Email("taylor.shah@agdata.com"));
        Assert.Equal(existing.EmployeeId.Value, allReference!.EmployeeId.Value);
    }

    [Fact]
    public async Task ProvisionUserAsync_WithInvalidEmail_ShouldThrow()
    {
        var service = BuildService();

        await Assert.ThrowsAsync<DomainException>(() => service.ProvisionUserAsync("Evelyn", "not-an-email", "AGD-030"));
    }
}
