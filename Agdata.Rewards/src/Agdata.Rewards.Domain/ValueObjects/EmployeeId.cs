using System.Text.RegularExpressions;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.ValueObjects;

public sealed record EmployeeId
{
    private static readonly Regex Pattern = new(@"^[A-Z]{3}-\d+$", RegexOptions.Compiled);

    public string Value { get; }

    public EmployeeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrors.EmployeeIdRequired);
        }

        var trimmed = value.Trim().ToUpperInvariant();

        if (!Pattern.IsMatch(trimmed))
        {
            throw new DomainException(DomainErrors.EmployeeIdInvalidFormat);
        }

        Value = trimmed;
    }

    public override string ToString() => Value;
}
