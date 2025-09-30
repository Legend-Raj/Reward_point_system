using System;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory.Auth;
using Xunit;

namespace Agdata.Rewards.Tests.Infrastructure;

public class InMemoryAdminRegistryTests
{
    [Fact]
    public void Ctor_WithNoSeedAdmins_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => new InMemoryAdminRegistry(Array.Empty<string>()));
    }

    [Fact]
    public void IsAdmin_ShouldBeCaseInsensitiveForEmail()
    {
        var registry = new InMemoryAdminRegistry(new[] { "ops.admin@agdata.com" });

        Assert.True(registry.IsAdmin("OPS.ADMIN@AGDATA.COM", "AGD-ADMIN-500"));
        Assert.False(registry.IsAdmin("guest@agdata.com", "AGD-ADMIN-500"));
    }

    [Fact]
    public void IsAdmin_ShouldMatchByEmployeeId()
    {
        var registry = new InMemoryAdminRegistry(new[] { "AGD-ADMIN-510" });

        Assert.True(registry.IsAdmin("anyone@agdata.com", "AGD-ADMIN-510"));
    }

    [Fact]
    public void AddAdmin_ShouldExtendRegistry()
    {
        var registry = new InMemoryAdminRegistry(new[] { "catalog.admin@agdata.com" });

        registry.AddAdmin("AGD-ADMIN-520");

        Assert.True(registry.IsAdmin("anyone@agdata.com", "AGD-ADMIN-520"));
    }

    [Fact]
    public void RemoveAdmin_WhenMoreThanOne_ShouldSucceed()
    {
        var registry = new InMemoryAdminRegistry(new[] { "support.admin@agdata.com", "AGD-ADMIN-530" });

        registry.RemoveAdmin("support.admin@agdata.com");

        Assert.False(registry.IsAdmin("support.admin@agdata.com", "AGD-ADMIN-999"));
        Assert.True(registry.IsAdmin("anyone@agdata.com", "AGD-ADMIN-530"));
    }

    [Fact]
    public void RemoveAdmin_WhenLastAdmin_ShouldThrow()
    {
        var registry = new InMemoryAdminRegistry(new[] { "solo.admin@agdata.com" });

        Assert.Throws<DomainException>(() => registry.RemoveAdmin("solo.admin@agdata.com"));
    }
}
