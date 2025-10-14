using System;
using System.Collections.Generic;
using System.Linq;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class EventRepositoryInMemory : IEventRepository
{
    private readonly Dictionary<Guid, Event> _events = new();

    public Task<Event?> GetEventByIdAsync(Guid eventId)
    {
        _events.TryGetValue(eventId, out var foundEvent);
        return Task.FromResult(foundEvent);
    }

    public Task<IEnumerable<Event>> ListEventsAsync()
    {
        return Task.FromResult(_events.Values.AsEnumerable());
    }

    public void AddEvent(Event newEvent)
    {
        _events[newEvent.Id] = newEvent;
    }

    public void UpdateEvent(Event eventToUpdate)
    {
        _events[eventToUpdate.Id] = eventToUpdate;
    }
}