using System;
using System.Globalization;

namespace Agdata.Rewards.Domain.Extensions;

/// <summary>
/// Extension methods for string normalization and processing.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Collapses multiple whitespace characters into single spaces and trims the result.
    /// </summary>
    /// <param name="value">The string to process.</param>
    /// <returns>A string with collapsed whitespace.</returns>
    public static string CollapseWhitespace(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Normalizes a string by trimming and collapsing whitespace.
    /// </summary>
    /// <param name="value">The string to normalize.</param>
    /// <returns>A normalized string.</returns>
    public static string NormalizeText(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().CollapseWhitespace();
    }

    /// <summary>
    /// Normalizes a string for display purposes by collapsing whitespace and applying title case.
    /// </summary>
    /// <param name="value">The string to normalize.</param>
    /// <returns>A normalized string in title case.</returns>
    public static string NormalizeForDisplay(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var collapsed = value.CollapseWhitespace();
        var lowered = collapsed.ToLowerInvariant();
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lowered);
    }

  
    public static string NormalizeRequired(this string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "Required string cannot be null.");
        }

        return value.Trim();
    }

    /// <summary>
    /// Normalizes an optional string field by trimming and collapsing whitespace.
    /// Returns null if the string is null or contains only whitespace.
    /// </summary>
    /// <param name="value">The string to normalize.</param>
    /// <returns>A normalized string or null if the input was null or whitespace.</returns>
    public static string? NormalizeOptional(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
