using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
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

    public async Task<Guid> CreateEventAsync(Admin creator, string name, DateTimeOffset occurredAt)
    {
        var newEvent = Event.CreateNew(name, occurredAt);
        _eventRepository.Add(newEvent);
        await _unitOfWork.SaveChangesAsync();
        return newEvent.Id;
    }

    public async Task UpdateEventAsync(Admin editor, Guid eventId, string name, DateTimeOffset occurredAt)
    {
        var eventToUpdate = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new DomainException("Event not found.");

        eventToUpdate.UpdateEventName(name);
        eventToUpdate.ChangeEventDateTime(occurredAt);

        _eventRepository.Update(eventToUpdate);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateEventAsync(Admin editor, Guid eventId)
    {
        var eventToUpdate = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new DomainException("Event not found.");

        eventToUpdate.MakeInactive();

        _eventRepository.Update(eventToUpdate);
        await _unitOfWork.SaveChangesAsync();
    }
}