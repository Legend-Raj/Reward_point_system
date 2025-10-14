using System;
using System.Globalization;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.ValueObjects;

public sealed class PersonName : IEquatable<PersonName>
{
    public string FirstName { get; }
    public string? MiddleName { get; }
    public string LastName { get; }

    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";

    private PersonName(string firstName, string? middleName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException(DomainErrors.PersonName.FirstRequired);
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException(DomainErrors.PersonName.LastRequired);
        }

    FirstName = NormalizeComponent(firstName);
    MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : NormalizeComponent(middleName);
    LastName = NormalizeComponent(lastName);
    }

    public static PersonName Create(string firstName, string? middleName, string lastName)
        => new(firstName, middleName, lastName);

    public PersonName WithFirstName(string firstName) => Create(firstName, MiddleName, LastName);

    public PersonName WithMiddleName(string? middleName) => Create(FirstName, middleName, LastName);

    public PersonName WithLastName(string lastName) => Create(FirstName, MiddleName, lastName);

    public override string ToString() => FullName;

    public override bool Equals(object? obj) => obj is PersonName other && Equals(other);

    public bool Equals(PersonName? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(FirstName, other.FirstName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(MiddleName, other.MiddleName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(LastName, other.LastName, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            FirstName.ToLowerInvariant(),
            MiddleName?.ToLowerInvariant(),
            LastName.ToLowerInvariant());
    }

    private static string NormalizeComponent(string value)
    {
        var collapsed = CollapseWhitespace(value);
        var lowered = collapsed.ToLowerInvariant();
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lowered);
    }

    private static string CollapseWhitespace(string value)
    {
        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
