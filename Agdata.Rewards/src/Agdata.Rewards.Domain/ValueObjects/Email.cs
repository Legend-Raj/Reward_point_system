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
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrors.EmailRequired);
        }

        var trimmed = value.Trim();

        if (!Pattern.IsMatch(trimmed))
        {
            throw new DomainException(DomainErrors.InvalidEmailFormat);
        }

        Value = trimmed;
    }

    public override string ToString() => Value;
}
