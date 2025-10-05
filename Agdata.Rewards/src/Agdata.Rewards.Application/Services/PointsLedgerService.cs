using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Application.Services;

public class PointsLedgerService : IPointsLedgerService
{
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IPointsTransactionRepository _pointsTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PointsLedgerService(
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IPointsTransactionRepository pointsTransactionRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PointsTransaction> EarnAsync(Guid userId, Guid eventId, int points)
    {
        if (points <= 0)
        {
            throw new DomainException("Points must be a positive amount.");
        }

        var userAccount = await _userRepository.GetByIdAsync(userId)
            ?? throw new DomainException("Cannot allocate points to a non-existent user.");

        var rewardEvent = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new DomainException("Cannot allocate points for a non-existent event.");

        if (!userAccount.IsActive)
        {
            throw new DomainException("Points cannot be allocated to an inactive user account.");
        }

        if (!rewardEvent.IsActive)
        {
            throw new DomainException("Points cannot be allocated for an inactive event.");
        }

        userAccount.AddPoints(points);

        var pointsTransaction = new PointsTransaction(
            Guid.NewGuid(),
            userAccount.Id,
            TransactionType.Earn,
            points,
            DateTimeOffset.UtcNow,
            eventId: rewardEvent.Id
        );
        _pointsTransactionRepository.Add(pointsTransaction);

        _userRepository.Update(userAccount);

        await _unitOfWork.SaveChangesAsync();

        return pointsTransaction;
    }

    public async Task<IReadOnlyList<PointsTransaction>> GetUserTransactionHistoryAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new DomainException("Cannot fetch history for a non-existent user.");

        return await _pointsTransactionRepository.GetByUserIdAsync(user.Id);
    }

    public async Task<Event> CreateEventAsync(string name, bool isActive = true)
    {
        var newEvent = Event.CreateNew(name, DateTimeOffset.UtcNow);

        if (!isActive)
        {
            newEvent.MakeInactive();
        }

        _eventRepository.Add(newEvent);
        await _unitOfWork.SaveChangesAsync();
        return newEvent;
    }

    public async Task<IReadOnlyList<Event>> ListEventsAsync(bool onlyActive = true)
    {
        var events = await _eventRepository.GetAllAsync();
        var filtered = onlyActive ? events.Where(ev => ev.IsActive) : events;
        return filtered.ToList();
    }

    public async Task<Event> SetEventActiveAsync(Guid eventId, bool isActive)
    {
        var rewardEvent = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new DomainException("Event not found.");

        if (isActive)
        {
            rewardEvent.MakeActive();
        }
        else
        {
            rewardEvent.MakeInactive();
        }

        _eventRepository.Update(rewardEvent);
        await _unitOfWork.SaveChangesAsync();

        return rewardEvent;
    }
}