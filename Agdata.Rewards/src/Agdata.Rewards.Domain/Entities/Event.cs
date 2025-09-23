using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

public sealed class Event
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public bool IsActive { get; private set; }

    public Event(Guid id, string name, DateTimeOffset occurredAt, bool isActive = true)
    {
        if (id == Guid.Empty) 
        {
            throw new DomainException("Event Id cannot be empty.");
        }
        
        ValidateEventName(name);

        Id = id;
        Name = name.Trim();
        OccurredAt = occurredAt;
        IsActive = isActive;
    }

    public static Event CreateNewEvent(string name, DateTimeOffset occurredAt)
    {
        return new Event(Guid.NewGuid(), name, occurredAt);
    }

    public void UpdateEventName(string name)
    {
        ValidateEventName(name);
        Name = name.Trim();
    }

    public void ChangeEventDateTime(DateTimeOffset newWhen)
    {
        OccurredAt = newWhen;
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
            throw new DomainException("Event name is required.");
        }
    }

    public override string ToString()
    {
        return $"{Name} @ {OccurredAt:yyyy-MM-dd HH:mm} (Active: {IsActive})";
    }
}
