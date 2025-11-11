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
        var user = await _userRepository.GetUserByIdForUpdateAsync(userId);
        if (user == null)
        {
            throw new DomainException(DomainErrors.User.NotFound);
        }

        user.CreditPoints(points);
        return user;
    }

    public async Task<User> ReservePointsAsync(Guid userId, int points)
    {
        var user = await _userRepository.GetUserByIdForUpdateAsync(userId);
        if (user == null)
        {
            throw new DomainException(DomainErrors.User.NotFound);
        }

        user.ReservePoints(points);
        return user;
    }

    public async Task<User> ReleasePointsAsync(Guid userId, int points)
    {
        var user = await _userRepository.GetUserByIdForUpdateAsync(userId);
        if (user == null)
        {
            throw new DomainException(DomainErrors.User.NotFound);
        }

        user.ReleasePoints(points);
        return user;
    }

    public async Task<User> CapturePointsAsync(Guid userId, int points)
    {
        var user = await _userRepository.GetUserByIdForUpdateAsync(userId);
        if (user == null)
        {
            throw new DomainException(DomainErrors.User.NotFound);
        }

        user.CapturePoints(points);
        return user;
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

