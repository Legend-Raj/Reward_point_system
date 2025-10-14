using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Tests.Common;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class AdminTests
{
    [Fact]
    public void CreateNew_ShouldProduceAdminWithUserCapabilities()
    {
    var parts = NameTestHelper.Split("Grace Morgan");
    var admin = Admin.CreateNew(parts.First, parts.Middle, parts.Last, "grace.morgan@agdata.com", "AGD-001");

        Assert.IsType<Admin>(admin);
        Assert.True(admin.IsActive);
        Assert.Equal(0, admin.TotalPoints);
        Assert.Equal("grace.morgan@agdata.com", admin.Email.Value);
    Assert.Equal("AGD-001", admin.EmployeeId.Value);
    }

    [Fact]
    public void CreateNew_ShouldRespectUserStateToggles()
    {
    var parts = NameTestHelper.Split("Logan Reyes");
    var admin = Admin.CreateNew(parts.First, parts.Middle, parts.Last, "logan.reyes@agdata.com", "AGD-002");

    admin.Deactivate();
        Assert.False(admin.IsActive);

    admin.Activate();
        Assert.True(admin.IsActive);
    }
}
