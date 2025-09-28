using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IEventService
{
    Task<Guid> CreateEventAsync(Admin creator, string name, DateTimeOffset occurredAt);
    Task UpdateEventAsync(Admin editor, Guid eventId, string name, DateTimeOffset occurredAt);
    Task DeactivateEventAsync(Admin editor, Guid eventId);
    Task<IEnumerable<Event>> GetActiveEventsAsync();
    Task<IEnumerable<Event>> GetPastEventsAsync();
}