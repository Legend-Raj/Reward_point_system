using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Auth;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Agdata.Rewards.Tests.Common;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class UserProvisioningServiceTests
{
    private static UserProvisioningService BuildService(IAdminRegistry? registry = null, UserRepositoryInMemory? repository = null)
    {
        repository ??= new UserRepositoryInMemory();
        registry ??= new InMemoryAdminRegistry(new[] { "safety.admin@agdata.com", "AGD-001" });
        var unitOfWork = new InMemoryUnitOfWork();
        return new UserProvisioningService(repository, registry, unitOfWork);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldCreateStandardUser_WhenNotAdmin()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository: repository);

        var (first, middle, last) = NameTestHelper.Split("Riya Kapoor");
        var user = await service.ProvisionUserAsync(first, middle, last, "riya.kapoor@agdata.com", "AGD-010");

        Assert.NotNull(user);
        Assert.False(user is Admin);
        var retrieved = await repository.GetUserByEmailAsync(new Email("riya.kapoor@agdata.com"));
        Assert.Equal(user.Id, retrieved!.Id);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldCreateAdmin_WhenRegistryMatchesEmail()
    {
        var repository = new UserRepositoryInMemory();
        var registry = new InMemoryAdminRegistry(new[] { "ops.director@agdata.com" });
        var service = BuildService(registry, repository);

        var adminParts = NameTestHelper.Split("Samuel Ortiz");
        var user = await service.ProvisionUserAsync(adminParts.First, adminParts.Middle, adminParts.Last, "ops.director@agdata.com", "AGD-010");

        Assert.IsType<Admin>(user);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldCreateAdmin_WhenRegistryMatchesEmployeeId()
    {
        var repository = new UserRepositoryInMemory();
        var registry = new InMemoryAdminRegistry(new[] { "AGD-015" });
        var service = BuildService(registry, repository);

        var parts = NameTestHelper.Split("Morgan Lee");
        var user = await service.ProvisionUserAsync(parts.First, parts.Middle, parts.Last, "morgan.lee@agdata.com", "AGD-015");

        Assert.IsType<Admin>(user);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldReturnExistingUser_ByEmail()
    {
        var repository = new UserRepositoryInMemory();
        var existingParts = NameTestHelper.Split("Taylor Shah");
        var existing = User.CreateNew(existingParts.First, existingParts.Middle, existingParts.Last, "taylor.shah@agdata.com", "AGD-020");
        repository.AddUser(existing);
        var service = BuildService(repository: repository);

        var newHireParts = NameTestHelper.Split("New Hire");
        var result = await service.ProvisionUserAsync(newHireParts.First, newHireParts.Middle, newHireParts.Last, "taylor.shah@agdata.com", "AGD-999");

        Assert.Equal(existing.Id, result.Id);
    }

    [Fact]
    public async Task ProvisionUserAsync_ShouldReturnExistingUser_ByEmployeeId()
    {
        var repository = new UserRepositoryInMemory();
        var existingParts = NameTestHelper.Split("Jordan Miles");
        var existing = User.CreateNew(existingParts.First, existingParts.Middle, existingParts.Last, "jordan.miles@agdata.com", "AGD-030");
        repository.AddUser(existing);
        var service = BuildService(repository: repository);

        var newHireParts = NameTestHelper.Split("Alex Rivers");
        var result = await service.ProvisionUserAsync(newHireParts.First, newHireParts.Middle, newHireParts.Last, "alex.rivers@agdata.com", "AGD-030");

        Assert.Equal(existing.Id, result.Id);
    }

    [Fact]
    public async Task ProvisionUserAsync_WithInvalidEmail_ShouldThrow()
    {
        var service = BuildService();

        var parts = NameTestHelper.Split("Evelyn Test");
        await Assert.ThrowsAsync<DomainException>(() => service.ProvisionUserAsync(parts.First, parts.Middle, parts.Last, "not-an-email", "AGD-030"));
    }
}
