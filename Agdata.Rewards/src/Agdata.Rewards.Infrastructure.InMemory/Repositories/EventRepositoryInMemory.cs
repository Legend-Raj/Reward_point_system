using System.Linq;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class EventRepositoryInMemory : IEventRepository
{
    private readonly Dictionary<Guid, Event> _events = new();

    public Task<Event?> GetByIdAsync(Guid id)
    {
        _events.TryGetValue(id, out var foundEvent);
        return Task.FromResult(foundEvent);
    }

    public Task<IEnumerable<Event>> GetAllAsync()
    {
        return Task.FromResult(_events.Values.AsEnumerable());
    }

    public void Add(Event newEvent)
    {
        _events[newEvent.Id] = newEvent;
    }

    public void Update(Event eventToUpdate)
    {
        _events[eventToUpdate.Id] = eventToUpdate;
    }
}