using System.Linq;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Application.Services.Shared;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EventService(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Event> CreateEventAsync(Admin creator, string name, DateTimeOffset occursAt, bool isActive = true)
    {
    AdminGuard.EnsureActive(creator);
        EnsureOccursAt(occursAt);

        var newEvent = Event.CreateNew(name, occursAt);

        if (!isActive)
        {
            newEvent.MakeInactive();
        }

        _eventRepository.AddEvent(newEvent);
        await _unitOfWork.SaveChangesAsync();
        return newEvent;
    }

    public async Task<Event> UpdateEventAsync(Admin editor, Guid eventId, string name, DateTimeOffset occursAt)
    {
    AdminGuard.EnsureActive(editor);
        EnsureEventId(eventId);
        EnsureOccursAt(occursAt);

        var eventToUpdate = await _eventRepository.GetEventByIdAsync(eventId)
            ?? throw new DomainException(DomainErrors.Event.NotFound);

        eventToUpdate.AdjustDetails(name, occursAt);

        _eventRepository.UpdateEvent(eventToUpdate);
        await _unitOfWork.SaveChangesAsync();

        return eventToUpdate;
    }

    public async Task DeactivateEventAsync(Admin editor, Guid eventId)
    {
    AdminGuard.EnsureActive(editor);
        EnsureEventId(eventId);

        var eventToUpdate = await _eventRepository.GetEventByIdAsync(eventId)
            ?? throw new DomainException(DomainErrors.Event.NotFound);

        eventToUpdate.MakeInactive();

        _eventRepository.UpdateEvent(eventToUpdate);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Event> SetEventActiveStateAsync(Admin editor, Guid eventId, bool isActive)
    {
    AdminGuard.EnsureActive(editor);
        EnsureEventId(eventId);

        var eventToUpdate = await _eventRepository.GetEventByIdAsync(eventId)
            ?? throw new DomainException(DomainErrors.Event.NotFound);

        if (isActive)
        {
            eventToUpdate.MakeActive();
        }
        else
        {
            eventToUpdate.MakeInactive();
        }

        _eventRepository.UpdateEvent(eventToUpdate);
        await _unitOfWork.SaveChangesAsync();

        return eventToUpdate;
    }

    public async Task<Event?> GetEventByIdAsync(Guid eventId)
    {
        EnsureEventId(eventId);
        return await _eventRepository.GetEventByIdAsync(eventId);
    }

    public async Task<IEnumerable<Event>> GetActiveEventsAsync()
    {
        var events = await _eventRepository.ListEventsAsync();
        var now = DateTimeOffset.UtcNow;

        return events
            .Where(e => e.IsActive && e.OccursAt >= now)
            .OrderBy(e => e.OccursAt)
            .ToList();
    }

    public async Task<IEnumerable<Event>> GetPastEventsAsync()
    {
        var events = await _eventRepository.ListEventsAsync();
        var now = DateTimeOffset.UtcNow;

        return events
            .Where(e => e.OccursAt < now)
            .OrderByDescending(e => e.OccursAt)
            .ToList();
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        var events = await _eventRepository.ListEventsAsync();
        return events
            .OrderBy(e => e.OccursAt)
            .ToList();
    }

    private static void EnsureOccursAt(DateTimeOffset occursAt)
    {
        if (occursAt == default)
        {
            throw new DomainException(DomainErrors.Event.OccursAtRequired);
        }
    }

    private static void EnsureEventId(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException(DomainErrors.Event.IdRequired);
        }
    }
}