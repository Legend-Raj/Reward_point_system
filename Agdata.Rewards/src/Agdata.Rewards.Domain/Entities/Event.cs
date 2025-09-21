using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

/// <summary>
/// Event matlab company ki activities ya contests jo hote hain jisme users points earn kar sakte hain.
/// Jaise: "Hackathon Finals", "Townhall Quiz", "Q3 Innovation Day".
/// Basic rules:
/// - Name khali nahi hona chahiye
/// - OccurredAt mein event ka actual time hoga (past ya future dono ok hai)
/// - IsActive: active events UI mein dikhenge, inactive matlab archived ya hidden
/// </summary>
public sealed class Event
{
    // Event ki basic identity
    public Guid Id { get; }

    // Event ki details aur current state
    public string Name { get; private set; }

    /// <summary>Event kab hua hai ya hone wala hai (UTC recommended)</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Active events list mein dikhenge, inactive means archived hai</summary>
    public bool IsActive { get; private set; }

    // Constructor aur factory methods
    public Event(Guid id, string name, DateTimeOffset occurredAt, bool isActive = true)
    {
        if (id == Guid.Empty) throw new DomainException("Event Id cannot be empty.");
        ValidateName(name);

        Id = id;
        Name = name.Trim();
        OccurredAt = occurredAt;
        IsActive = isActive;
    }

    public static Event CreateNew(string name, DateTimeOffset occurredAt)
        => new(Guid.NewGuid(), name, occurredAt);

    // Business logic methods
    public void Rename(string name)
    {
        ValidateName(name);
        Name = name.Trim();
    }

    /// <summary>
    /// Admin agar date/time change karna chahta hai (reschedule ya typo fix ke liye).
    /// Past ya future dono ok hai, upcoming/past ka decision query layer decide karegi.
    /// </summary>
    public void Reschedule(DateTimeOffset newWhen)
        => OccurredAt = newWhen;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    // Validation helpers
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Event name is required.");
    }

    public override string ToString()
        => $"{Name} @ {OccurredAt:yyyy-MM-dd HH:mm} (Active: {IsActive})";
}
