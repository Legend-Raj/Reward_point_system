using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Application.Services;

public class PointsService : IPointsService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PointsService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<User> CreditPointsAsync(Guid userId, int points)
    {
        if (points <= 0)
        {
            throw new DomainException(DomainErrors.User.CreditAmountMustBePositive);
        }

        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new DomainException(DomainErrors.User.NotFound);
        }

        if (!user.IsActive)
        {
            throw new DomainException(DomainErrors.User.AllocationBlockedInactiveAccount);
        }

        var updatedUser = new User(
            user.Id,
            user.Name,
            user.Email,
            user.EmployeeId,
            user.IsActive,
            user.TotalPoints + points,
            user.LockedPoints,
            user.CreatedAt,
            DateTimeOffset.UtcNow
        );

        _userRepository.UpdateUser(updatedUser);
        // Note: Caller is responsible for calling SaveChangesAsync to control transaction boundary

        return updatedUser;
    }

    public async Task<User> ReservePointsAsync(Guid userId, int points)
    {
        if (points <= 0)
        {
            throw new DomainException(DomainErrors.User.ReserveAmountMustBePositive);
        }

        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new DomainException(DomainErrors.User.NotFound);
        }

        if (user.AvailablePoints < points)
        {
            throw new DomainException(DomainErrors.User.InsufficientPointsToReserve);
        }

        var updatedUser = new User(
            user.Id,
            user.Name,
            user.Email,
            user.EmployeeId,
            user.IsActive,
            user.TotalPoints,
            user.LockedPoints + points,
            user.CreatedAt,
            DateTimeOffset.UtcNow
        );

        _userRepository.UpdateUser(updatedUser);
        return updatedUser;
    }

    public async Task<User> ReleasePointsAsync(Guid userId, int points)
    {
        if (points <= 0)
        {
            throw new DomainException(DomainErrors.User.ReleaseAmountMustBePositive);
        }

        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new DomainException(DomainErrors.User.NotFound);
        }

        if (user.LockedPoints < points)
        {
            throw new DomainException(DomainErrors.User.ReleaseExceedsReserved);
        }

        var updatedUser = new User(
            user.Id,
            user.Name,
            user.Email,
            user.EmployeeId,
            user.IsActive,
            user.TotalPoints,
            user.LockedPoints - points,
            user.CreatedAt,
            DateTimeOffset.UtcNow
        );

        _userRepository.UpdateUser(updatedUser);
        return updatedUser;
    }

    public async Task<User> CapturePointsAsync(Guid userId, int points)
    {
        if (points <= 0)
        {
            throw new DomainException(DomainErrors.User.CaptureAmountMustBePositive);
        }

        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new DomainException(DomainErrors.User.NotFound);
        }

        if (user.LockedPoints < points)
        {
            throw new DomainException(DomainErrors.User.CaptureExceedsReserved);
        }

        var updatedUser = new User(
            user.Id,
            user.Name,
            user.Email,
            user.EmployeeId,
            user.IsActive,
            user.TotalPoints - points,
            user.LockedPoints - points,
            user.CreatedAt,
            DateTimeOffset.UtcNow
        );

        _userRepository.UpdateUser(updatedUser);
        return updatedUser;
    }

    public async Task<(int TotalPoints, int LockedPoints, int AvailablePoints)> GetPointsBalanceAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new DomainException(DomainErrors.User.NotFound);
        }

        return (user.TotalPoints, user.LockedPoints, user.AvailablePoints);
    }

    public async Task<bool> HasSufficientPointsAsync(Guid userId, int requiredPoints)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        return user.AvailablePoints >= requiredPoints;
    }
}

