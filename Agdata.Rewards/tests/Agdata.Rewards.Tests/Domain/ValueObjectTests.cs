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
    }

    [Fact]
    public void Email_WithSurroundingWhitespace_ShouldTrim()
    {
        var email = new Email("   analyst@agdata.com   ");
        Assert.Equal("analyst@agdata.com", email.Value);
    }

    [Fact]
    public void EmployeeId_ShouldTrimAndValidate()
    {
        var employeeId = new EmployeeId("  agd-77  ");
        Assert.Equal("AGD-77", employeeId.Value);

        Assert.Throws<DomainException>(() => new EmployeeId(" "));
        Assert.Throws<DomainException>(() => new EmployeeId("AGD77"));
        Assert.Throws<DomainException>(() => new EmployeeId("AG-123"));
    }

    [Fact]
    public void Email_ShouldThrow_OnEmpty_And_Invalid()
    {
        Assert.Throws<DomainException>(() => new Email(""));
        Assert.Throws<DomainException>(() => new Email("invalid"));
    }

    [Theory]
    [InlineData("userexample.com")]
    [InlineData("user@domain")]
    [InlineData("@missinglocal.com")]
    [InlineData("user@.com")]
    public void Email_ShouldThrow_OnInvalidFormats(string input)
    {
        Assert.Throws<DomainException>(() => new Email(input));
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

    [Theory]
    [InlineData("", "Last")]
    [InlineData("First", "")]
    [InlineData("   ", "Last")]
    [InlineData("First", "   ")]
    public void PersonName_ShouldThrow_OnMissingParts(string first, string last)
    {
        Assert.Throws<DomainException>(() => PersonName.Create(first, null, last));
    }

    [Theory]
    [InlineData("")]
    [InlineData("AGD123")]
    [InlineData("AAA-12A")]
    public void EmployeeId_ShouldThrow_OnInvalidFormats(string input)
    {
        Assert.Throws<DomainException>(() => new EmployeeId(input));
    }
}
