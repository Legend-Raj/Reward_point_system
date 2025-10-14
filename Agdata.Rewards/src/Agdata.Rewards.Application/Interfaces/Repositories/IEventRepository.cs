using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IEventRepository
{
    /// <summary>Fetches a single event by its identifier.</summary>
    Task<Event?> GetEventByIdAsync(Guid eventId);

    /// <summary>Enumerates events stored in the repository.</summary>
    Task<IEnumerable<Event>> ListEventsAsync();

    /// <summary>Persists a newly created event.</summary>
    void AddEvent(Event newEvent);

    /// <summary>Persists changes to an existing event.</summary>
    void UpdateEvent(Event eventToUpdate);
}