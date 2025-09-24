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

    public async Task<Guid> AllocatePointsToUserForEventAsync(Admin allocator, Guid userId, Guid eventId, int points)
    {
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

        return pointsTransaction.Id;
    }

    public Task<IEnumerable<PointsTransaction>> GetUserTransactionHistoryAsync(Guid userId)
    {
        throw new NotImplementedException("Add IPointsTransactionRepository.GetByUserIdAsync to surface history.");
    }
}