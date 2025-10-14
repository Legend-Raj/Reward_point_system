using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Infrastructure.InMemory.Auth;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Agdata.Rewards.Tests.Common;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class AuthServiceTests
{
    private static AuthService BuildService(IAdminRegistry? registry = null, UserRepositoryInMemory? repository = null)
    {
        repository ??= new UserRepositoryInMemory();
        registry ??= new InMemoryAdminRegistry(new[] { "security.admin@agdata.com", "AGD-001" });
        return new AuthService(repository, registry);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnUser_WhenEmailMatches()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository: repository);

        var parts = NameTestHelper.Split("Priya Mehta");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "priya.mehta@agdata.com", "AGD-301");
        repository.AddUser(user);

        var authenticated = await service.AuthenticateAsync("priya.mehta@agdata.com", "AGD-999");

        Assert.Equal(user.Id, authenticated.Id);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnUser_WhenEmployeeIdMatches()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository: repository);

        var parts = NameTestHelper.Split("Noah Clark");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "noah.clark@agdata.com", "AGD-555");
        repository.AddUser(user);

        var authenticated = await service.AuthenticateAsync("someoneelse@agdata.com", "AGD-555");

        Assert.Equal(user.Id, authenticated.Id);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCredentialsMismatch_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository: repository);

        var parts = NameTestHelper.Split("Jordan Fields");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "jordan.fields@agdata.com", "AGD-707");
        repository.AddUser(user);

        await Assert.ThrowsAsync<DomainException>(() => service.AuthenticateAsync("wrong.email@agdata.com", "AGD-000"));
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserInactive_ShouldThrow()
    {
        var repository = new UserRepositoryInMemory();
        var service = BuildService(repository: repository);

        var parts = NameTestHelper.Split("Inactive User");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "inactive.user@agdata.com", "AGD-808");
        user.Deactivate();
        repository.AddUser(user);

        await Assert.ThrowsAsync<DomainException>(() => service.AuthenticateAsync("inactive.user@agdata.com", "AGD-808"));
    }

    [Fact]
    public void IsAdmin_ShouldReflectRegistry()
    {
        var registry = new InMemoryAdminRegistry(new[] { "ops.director@agdata.com", "AGD-010" });
        var service = BuildService(registry, new UserRepositoryInMemory());

        Assert.True(service.IsAdmin("ops.director@agdata.com", "AGD-999"));
        Assert.True(service.IsAdmin("someone@agdata.com", "AGD-010"));
        Assert.False(service.IsAdmin("user@agdata.com", "AGD-888"));
    }
}
