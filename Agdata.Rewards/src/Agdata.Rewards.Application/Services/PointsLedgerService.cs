using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.DTOs.Common;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Application.Services.Shared;

namespace Agdata.Rewards.Application.Services;

public class PointsLedgerService : IPointsLedgerService
{
    private const int MaxPageSize = 100;

    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILedgerEntryRepository _ledgerEntryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PointsLedgerService(
        IUserRepository userRepository,
    IEventRepository eventRepository,
    ILedgerEntryRepository ledgerEntryRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LedgerEntry> EarnAsync(Admin actor, Guid userId, Guid eventId, int points)
    {
    AdminGuard.EnsureActive(actor);

        if (points <= 0)
        {
            throw new DomainException(DomainErrors.Points.MustBePositive);
        }

        var userAccount = await _userRepository.GetUserByIdAsync(userId)
            ?? throw new DomainException(DomainErrors.Repository.NonExistentUser);

        var rewardEvent = await _eventRepository.GetEventByIdAsync(eventId)
            ?? throw new DomainException(DomainErrors.Repository.NonExistentEvent);

        if (!userAccount.IsActive)
        {
            throw new DomainException(DomainErrors.User.AllocationBlockedInactiveAccount);
        }

        if (!rewardEvent.IsActive)
        {
            throw new DomainException(DomainErrors.Event.Inactive);
        }

        userAccount.CreditPoints(points);

        var ledgerEntry = new LedgerEntry(
            Guid.NewGuid(),
            userAccount.Id,
            LedgerEntryType.Earn,
            points,
            DateTimeOffset.UtcNow,
            eventId: rewardEvent.Id
        );
        _ledgerEntryRepository.AddLedgerEntry(ledgerEntry);

        _userRepository.UpdateUser(userAccount);

        await _unitOfWork.SaveChangesAsync();

        return ledgerEntry;
    }

    public async Task<PagedResult<LedgerEntry>> GetUserTransactionHistoryAsync(Guid userId, int skip, int take)
    {
        if (skip < 0)
        {
            throw new DomainException(DomainErrors.Validation.SkipMustBeNonNegative);
        }

        if (take <= 0)
        {
            throw new DomainException(DomainErrors.Validation.TakeMustBePositive);
        }

        if (take > MaxPageSize)
        {
            throw new DomainException(DomainErrors.Validation.TakeExceedsMaximum);
        }

        var user = await _userRepository.GetUserByIdAsync(userId)
            ?? throw new DomainException(DomainErrors.History.UserMissing);

        var history = await _ledgerEntryRepository.ListLedgerEntriesByUserAsync(user.Id);
        var ordered = history.OrderByDescending(tx => tx.Timestamp).ToList();
        var page = ordered.Skip(skip).Take(take).ToList();

        return new PagedResult<LedgerEntry>(page, ordered.Count, skip, take);
    }

}