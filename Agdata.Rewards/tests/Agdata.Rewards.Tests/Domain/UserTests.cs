using System;
using System.Threading;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Tests.Common;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class UserTests
{
    [Fact]
    public void CreateNew_ShouldInitialiseUserWithDefaults()
    {
        var parts = NameTestHelper.Split("Alex Reed");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "alex.reed@agdata.com", "AGD-001");

        Assert.Equal("Alex", user.Name.FirstName);
        Assert.Equal("Reed", user.Name.LastName);
        Assert.Equal("Alex Reed", user.Name.FullName);
        Assert.True(user.IsActive);
        Assert.Equal(0, user.TotalPoints);
        Assert.Equal(0, user.LockedPoints);
        Assert.Equal(user.TotalPoints - user.LockedPoints, user.AvailablePoints);
        Assert.Equal("alex.reed@agdata.com", user.Email.Value);
        Assert.Equal("AGD-001", user.EmployeeId.Value);
        Assert.True(user.CreatedAt <= user.UpdatedAt);
        Assert.True((DateTimeOffset.UtcNow - user.CreatedAt) < TimeSpan.FromSeconds(5));
    }


    [Fact]
    public void Rename_ShouldTrimAndPersist()
    {
        var parts = NameTestHelper.Split("Fiona Grey");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "fiona.grey@agdata.com", "AGD-500");
        var updatedName = PersonName.Create("New", null, "Name");

        user.Rename(updatedName);

        Assert.Equal("New", user.Name.FirstName);
        Assert.Equal("Name", user.Name.LastName);
        Assert.Equal("New Name", user.Name.FullName);
    }

    [Fact]
    public void Mutations_ShouldRefreshUpdatedAt()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var initialName = PersonName.Create("Audit", null, "User");
        var user = new User(
            Guid.NewGuid(),
            initialName,
            new Email("audit.user@agdata.com"),
            new EmployeeId("AGD-777"),
            createdAt: createdAt,
            updatedAt: createdAt);

        user.Rename(PersonName.Create("Audit", null, "Update"));

        Assert.True(user.UpdatedAt > createdAt);

        var afterNameUpdate = user.UpdatedAt;

        // Points operations moved to PointsService
    }

    [Fact]
    public void Rename_ShouldAdvanceUpdatedAt()
    {
        var parts = NameTestHelper.Split("Logan Pierce");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "logan.pierce@agdata.com", "AGD-710");

        var before = user.UpdatedAt;
        Thread.Sleep(5);
        user.Rename(PersonName.Create("Logan", "A.", "Pierce"));

        Assert.True(user.UpdatedAt > before);
    }

    [Fact]
    public void ChangeEmail_ShouldAdvanceUpdatedAt()
    {
        var parts = NameTestHelper.Split("Avery Nolan");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "avery.nolan@agdata.com", "AGD-712");

        var before = user.UpdatedAt;
        Thread.Sleep(5);
        var newEmail = new Email("avery.n@agdata.com");

        user.ChangeEmail(newEmail);

        Assert.Equal("avery.n@agdata.com", user.Email.Value);
        Assert.True(user.UpdatedAt > before);
    }

    [Fact]
    public void ChangeEmail_WhenNull_ShouldThrow()
    {
        var parts = NameTestHelper.Split("Jordan Blake");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "jordan.blake@agdata.com", "AGD-713");

        Assert.Throws<DomainException>(() => user.ChangeEmail(null!));
    }

    [Fact]
    public void ChangeEmployeeId_ShouldAdvanceUpdatedAt()
    {
        var parts = NameTestHelper.Split("Lena Ortiz");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "lena.ortiz@agdata.com", "AGD-714");

        var before = user.UpdatedAt;
        Thread.Sleep(5);
        var newId = new EmployeeId("agd-902");

        user.ChangeEmployeeId(newId);

        Assert.Equal("AGD-902", user.EmployeeId.Value);
        Assert.True(user.UpdatedAt > before);
    }

    [Fact]
    public void ChangeEmployeeId_WhenNull_ShouldThrow()
    {
        var parts = NameTestHelper.Split("Morgan Tate");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "morgan.tate@agdata.com", "AGD-715");

        Assert.Throws<DomainException>(() => user.ChangeEmployeeId(null!));
    }

    [Fact]
    public void Constructor_WithInvalidArguments_ShouldThrow()
    {
        var email = new Email("testing@agdata.com");
        var employeeId = new EmployeeId("AGD-900");
        var name = PersonName.Create("Test", null, "User");

        Assert.Throws<DomainException>(() => new User(Guid.Empty, name, email, employeeId));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), null!, email, employeeId));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), name, email, employeeId, totalPoints: -1));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), name, email, employeeId, totalPoints: 10, lockedPoints: 20));
    }


    [Fact]
    public void ActivationFlow_ShouldToggleStates()
    {
        var parts = NameTestHelper.Split("Kara Mills");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "kara.mills@agdata.com", "AGD-640");

        user.Deactivate();
        Assert.False(user.IsActive);

        user.Activate();
        Assert.True(user.IsActive);
    }

}
