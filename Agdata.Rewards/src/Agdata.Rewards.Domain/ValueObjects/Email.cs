using System.Text.RegularExpressions;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.ValueObjects;

public sealed record Email
{
    private static readonly Regex Pattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Pattern.IsMatch(value))
        {
            throw new DomainException("Invalid email address.");
        }
        Value = value.Trim();
    }

    public override string ToString() => Value;
}
