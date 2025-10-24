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
    private readonly IPointsService _pointsService;
    private readonly IUnitOfWork _unitOfWork;

    public PointsLedgerService(
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILedgerEntryRepository ledgerEntryRepository,
        IPointsService pointsService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _pointsService = pointsService;
        _unitOfWork = unitOfWork;
    }

    public async Task<LedgerEntry> EarnAsync(Admin actor, Guid userId, Guid eventId, int points)
    {
        AdminGuard.EnsureActive(actor);
        Guard.AgainstNonPositive(points, DomainErrors.Points.MustBePositive);

        const int maxRetries = 3;
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var userAccount = await _userRepository.GetUserByIdAsync(userId)
                    ?? throw new DomainException(DomainErrors.Repository.NonExistentUser);

                if (!userAccount.IsActive)
                {
                    throw new DomainException(DomainErrors.User.AllocationBlockedInactiveAccount);
                }

                var rewardEvent = await _eventRepository.GetEventByIdAsync(eventId)
                    ?? throw new DomainException(DomainErrors.Repository.NonExistentEvent);

                if (!rewardEvent.IsActive)
                {
                    throw new DomainException(DomainErrors.Event.Inactive);
                }

                await _pointsService.CreditPointsAsync(userAccount.Id, points);

                var ledgerEntry = new LedgerEntry(
                    Guid.NewGuid(),
                    userAccount.Id,
                    LedgerEntryType.Earn,
                    points,
                    DateTimeOffset.UtcNow,
                    eventId: rewardEvent.Id
                );
                _ledgerEntryRepository.AddLedgerEntry(ledgerEntry);

                await _unitOfWork.SaveChangesAsync();
                return ledgerEntry;
            }
            catch (Exception ex) when (attempt < maxRetries - 1 && IsConcurrencyException(ex))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)));
                continue;
            }
        }
        
        throw new DomainException("Unable to credit points due to concurrent modifications. Please try again.", 409);
    }

    private static bool IsConcurrencyException(Exception ex)
    {
        var typeName = ex.GetType().Name;
        return typeName.Contains("Concurrency", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("DbUpdate", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<PagedResult<LedgerEntry>> GetUserTransactionHistoryAsync(Guid userId, int skip, int take)
    {
       
        Guard.AgainstNegativeSkip(skip);
        Guard.AgainstInvalidTake(take, MaxPageSize);

        var user = await _userRepository.GetUserByIdAsync(userId)
            ?? throw new DomainException(DomainErrors.History.UserMissing);

        var history = await _ledgerEntryRepository.ListLedgerEntriesByUserAsync(user.Id);
        var ordered = history
            .OrderByDescending(tx => tx.Timestamp)
            .ThenBy(tx => tx.Id) 
            .ToList();
        var page = ordered.Skip(skip).Take(take).ToList();

        return new PagedResult<LedgerEntry>(page, ordered.Count, skip, take);
    }

}