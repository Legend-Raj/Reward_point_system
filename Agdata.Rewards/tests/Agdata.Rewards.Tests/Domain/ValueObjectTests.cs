using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void Email_ShouldValidateFormat()
    {
        var email = new Email("person@example.com");
        Assert.Equal("person@example.com", email.Value);

        Assert.Throws<DomainException>(() => new Email("not-an-email"));
        Assert.Throws<DomainException>(() => new Email(" "));
    }

    [Fact]
    public void Email_WithSurroundingWhitespace_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => new Email("   analyst@agdata.com   "));
    }

    [Fact]
    public void EmployeeId_ShouldTrimAndValidate()
    {
        var employeeId = new EmployeeId("  EMP-77  ");
        Assert.Equal("EMP-77", employeeId.Value);

        Assert.Throws<DomainException>(() => new EmployeeId(" "));
    }

    [Fact]
    public void Equality_ShouldBeByValue()
    {
        var email1 = new Email("a@b.com");
        var email2 = new Email("a@b.com");
        var email3 = new Email("c@d.com");

        Assert.Equal(email1, email2);
        Assert.NotEqual(email1, email3);
    }
}
