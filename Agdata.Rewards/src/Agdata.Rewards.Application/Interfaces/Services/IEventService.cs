using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IEventService
{
    Task<Event> CreateEventAsync(Admin creator, string name, DateTimeOffset occursAt, bool isActive = true);
    Task<Event> UpdateEventAsync(Admin editor, Guid eventId, string name, DateTimeOffset occursAt);
    Task DeactivateEventAsync(Admin editor, Guid eventId);
    Task<Event> SetEventActiveStateAsync(Admin editor, Guid eventId, bool isActive);
    Task<Event?> GetEventByIdAsync(Guid eventId);
    Task<IEnumerable<Event>> GetActiveEventsAsync();
    Task<IEnumerable<Event>> GetPastEventsAsync();
    Task<IEnumerable<Event>> GetAllEventsAsync();
}