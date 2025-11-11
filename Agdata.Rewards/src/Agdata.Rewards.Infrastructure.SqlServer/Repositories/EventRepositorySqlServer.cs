using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Infrastructure.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Agdata.Rewards.Infrastructure.SqlServer.Repositories;

public class EventRepositorySqlServer : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepositorySqlServer(AppDbContext context)
    {
        _context = context;
    }

    public Task<Event?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
    }

    public async Task<IEnumerable<Event>> ListEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public void AddEvent(Event newEvent)
    {
        _context.Events.Add(newEvent);
    }

    public void UpdateEvent(Event eventToUpdate)
    {
        _context.Events.Update(eventToUpdate);
    }
}

