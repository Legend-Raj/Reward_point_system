using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

public sealed class Event
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public DateTimeOffset OccursAt { get; private set; }
    public bool IsActive { get; private set; }

    public Event(Guid eventId, string name, DateTimeOffset occursAt, bool isActive = true)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException(DomainErrors.Event.IdRequired);
        }

        ValidateEventName(name);

    Id = eventId;
        Name = name.Trim();
        OccursAt = occursAt;
        IsActive = isActive;
    }

    public static Event CreateNew(string name, DateTimeOffset occursAt)
    {
        return new Event(Guid.NewGuid(), name, occursAt);
    }

    public void AdjustDetails(string name, DateTimeOffset occursAt)
    {
        ValidateEventName(name);
        Name = name.Trim();
        OccursAt = occursAt;
    }

    public void MakeActive()
    {
        IsActive = true;
    }

    public void MakeInactive()
    {
        IsActive = false;
    }

    private static void ValidateEventName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrors.Event.NameRequired);
        }
    }

    public override string ToString()
    {
        return $"{Name} @ {OccursAt:yyyy-MM-dd HH:mm} (Active: {IsActive})";
    }
}
