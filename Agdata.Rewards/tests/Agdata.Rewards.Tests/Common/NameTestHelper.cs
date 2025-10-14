using System;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Tests.Common;

internal static class NameTestHelper
{
    internal static (string First, string? Middle, string Last) Split(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name must not be empty.", nameof(fullName));
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            throw new ArgumentException("Full name must contain at least a first and last name.", nameof(fullName));
        }

        var first = parts[0];
        var last = parts[^1];
        string? middle = parts.Length > 2 ? string.Join(" ", parts[1..^1]) : null;

        return (first, middle, last);
    }

    internal static PersonName CreatePersonName(string fullName)
    {
        var (first, middle, last) = Split(fullName);
        return PersonName.Create(first, middle, last);
    }
}
