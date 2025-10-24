using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IEventRepository
{
    /// <summary>Fetches a single event by its identifier.</summary>
    Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Enumerates events stored in the repository.</summary>
    Task<IEnumerable<Event>> ListEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a newly created event.</summary>
    void AddEvent(Event newEvent);

    /// <summary>Persists changes to an existing event.</summary>
    void UpdateEvent(Event eventToUpdate);
}