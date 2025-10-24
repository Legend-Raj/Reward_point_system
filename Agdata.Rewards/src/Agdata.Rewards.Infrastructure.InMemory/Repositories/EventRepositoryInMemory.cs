using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class EventRepositoryInMemory : IEventRepository
{
    private readonly Dictionary<Guid, Event> _events = new();
    private readonly object _gate = new();

    public Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _events.TryGetValue(eventId, out var foundEvent);
            return Task.FromResult(foundEvent);
        }
    }

    public Task<IEnumerable<Event>> ListEventsAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_events.Values.ToList().AsEnumerable());
        }
    }

    public void AddEvent(Event newEvent)
    {
        lock (_gate)
        {
            _events[newEvent.Id] = newEvent;
        }
    }

    public void UpdateEvent(Event eventToUpdate)
    {
        lock (_gate)
        {
            _events[eventToUpdate.Id] = eventToUpdate;
        }
    }
}