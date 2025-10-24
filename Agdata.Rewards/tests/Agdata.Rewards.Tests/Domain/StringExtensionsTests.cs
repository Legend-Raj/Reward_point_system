using Agdata.Rewards.Domain.Extensions;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

/// <summary>
/// Tests for StringExtensions focusing on null safety and edge cases.
/// </summary>
public class StringExtensionsTests
{
    #region NormalizeRequired Tests

    [Fact]
    public void NormalizeRequired_WithValidString_ShouldTrim()
    {
        // Arrange
        var input = "  Hello World  ";

        // Act
        var result = input.NormalizeRequired();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void NormalizeRequired_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => input!.NormalizeRequired());
        Assert.Contains("Required string cannot be null", exception.Message);
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void NormalizeRequired_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.NormalizeRequired();

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void NormalizeRequired_WithWhitespaceOnly_ShouldReturnEmpty()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = input.NormalizeRequired();

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void NormalizeRequired_WithNoWhitespace_ShouldReturnSame()
    {
        // Arrange
        var input = "Hello";

        // Act
        var result = input.NormalizeRequired();

        // Assert
        Assert.Equal("Hello", result);
    }

    [Theory]
    [InlineData(" John ", "John")]
    [InlineData("  Jane  ", "Jane")]
    [InlineData("\tBob\t", "Bob")]
    [InlineData("\nAlice\n", "Alice")]
    [InlineData(" Leading", "Leading")]
    [InlineData("Trailing ", "Trailing")]
    public void NormalizeRequired_WithVariousWhitespace_ShouldTrimCorrectly(string input, string expected)
    {
        // Act
        var result = input.NormalizeRequired();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region NormalizeOptional Tests

    [Fact]
    public void NormalizeOptional_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input.NormalizeOptional();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NormalizeOptional_WithWhitespaceOnly_ShouldReturnNull()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = input.NormalizeOptional();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NormalizeOptional_WithValidString_ShouldTrim()
    {
        // Arrange
        var input = "  Middle Name  ";

        // Act
        var result = input.NormalizeOptional();

        // Assert
        Assert.Equal("Middle Name", result);
    }

    [Fact]
    public void NormalizeOptional_WithEmptyString_ShouldReturnNull()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.NormalizeOptional();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CollapseWhitespace Tests

    [Fact]
    public void CollapseWhitespace_WithMultipleSpaces_ShouldCollapseToSingle()
    {
        // Arrange
        var input = "Hello    World";

        // Act
        var result = input.CollapseWhitespace();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void CollapseWhitespace_WithMixedWhitespace_ShouldCollapseToSingle()
    {
        // Arrange
        var input = "Hello\t\n  World";

        // Act
        var result = input.CollapseWhitespace();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void CollapseWhitespace_WithLeadingAndTrailing_ShouldTrimAndCollapse()
    {
        // Arrange
        var input = "  Hello   World  ";

        // Act
        var result = input.CollapseWhitespace();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void CollapseWhitespace_WithNull_ShouldReturnEmpty()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input!.CollapseWhitespace();

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void CollapseWhitespace_WithWhitespaceOnly_ShouldReturnEmpty()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = input.CollapseWhitespace();

        // Assert
        Assert.Equal("", result);
    }

    #endregion

    #region NormalizeText Tests

    [Fact]
    public void NormalizeText_ShouldTrimAndCollapseWhitespace()
    {
        // Arrange
        var input = "  Hello    World  ";

        // Act
        var result = input.NormalizeText();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void NormalizeText_WithNull_ShouldReturnEmpty()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input!.NormalizeText();

        // Assert
        Assert.Equal("", result);
    }

    #endregion

    #region NormalizeForDisplay Tests

    [Fact]
    public void NormalizeForDisplay_ShouldApplyTitleCase()
    {
        // Arrange
        var input = "  hello    world  ";

        // Act
        var result = input.NormalizeForDisplay();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void NormalizeForDisplay_WithMixedCase_ShouldStandardize()
    {
        // Arrange
        var input = "joHN dOE";

        // Act
        var result = input.NormalizeForDisplay();

        // Assert
        Assert.Equal("John Doe", result);
    }

    [Fact]
    public void NormalizeForDisplay_WithNull_ShouldReturnEmpty()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input!.NormalizeForDisplay();

        // Assert
        Assert.Equal("", result);
    }

    #endregion

    #region Real-World Scenarios

    [Theory]
    [InlineData("García", "García")]  // Accented characters
    [InlineData("O'Brien", "O'Brien")]  // Apostrophes
    [InlineData("van der Berg", "van der Berg")]  // Compound names
    [InlineData("José María", "José María")]  // Multiple accents
    public void NormalizeRequired_WithInternationalNames_ShouldPreserveCharacters(string input, string expected)
    {
        // Act
        var result = input.NormalizeRequired();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeRequired_WithUnicodeWhitespace_ShouldTrim()
    {
        // Arrange - Using Unicode non-breaking space (U+00A0)
        var input = "\u00A0Hello\u00A0";

        // Act
        var result = input.NormalizeRequired();

        // Assert
        Assert.Equal("Hello", result);
    }

    [Theory]
    [InlineData("  First Name  ", "First Name")]
    [InlineData(" Last  Name ", "Last  Name")]  // Internal spaces preserved, edges trimmed
    [InlineData("NoSpaces", "NoSpaces")]
    public void NormalizeRequired_WithCommonFormInputs_ShouldCleanCorrectly(string input, string expected)
    {
        // Act
        var result = input.NormalizeRequired();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion
}

