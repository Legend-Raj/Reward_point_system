using Agdata.Rewards.Domain.Entities;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class AdminTests
{
    [Fact]
    public void CreateNew_ShouldProduceAdminWithUserCapabilities()
    {
        var admin = Admin.CreateNew("Grace Morgan", "grace.morgan@agdata.com", "AGD-ADMIN-01");

        Assert.IsType<Admin>(admin);
        Assert.True(admin.IsActive);
        Assert.Equal(0, admin.TotalPoints);
        Assert.Equal("grace.morgan@agdata.com", admin.Email.Value);
        Assert.Equal("AGD-ADMIN-01", admin.EmployeeId.Value);
    }

    [Fact]
    public void CreateNew_ShouldRespectUserStateToggles()
    {
        var admin = Admin.CreateNew("Logan Reyes", "logan.reyes@agdata.com", "AGD-ADMIN-02");

        admin.DeactivateAccount();
        Assert.False(admin.IsActive);

        admin.ActivateAccount();
        Assert.True(admin.IsActive);
    }
}
