using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.ValueObjects;

public sealed record EmployeeId
{
    public string Value { get; }

    public EmployeeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("EmployeeId cannot be empty.");
        }
        Value = value.Trim();
    }

    public override string ToString() => Value;
}
