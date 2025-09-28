using Agdata.Rewards.Domain.Entities;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class AdminTests
{
    [Fact]
    public void CreateNew_ShouldProduceAdminWithUserCapabilities()
    {
        var admin = Admin.CreateNew("Principal", "principal@school.com", "ADMIN-1");

        Assert.IsType<Admin>(admin);
        Assert.True(admin.IsActive);
        Assert.Equal(0, admin.TotalPoints);
        Assert.Equal("principal@school.com", admin.Email.Value);
        Assert.Equal("ADMIN-1", admin.EmployeeId.Value);
    }
}
