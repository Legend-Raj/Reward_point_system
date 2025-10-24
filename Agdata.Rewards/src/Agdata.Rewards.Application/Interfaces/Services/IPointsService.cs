using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

/// <summary>
/// Service for managing user points operations.
/// </summary>
public interface IPointsService
{
    /// <summary>
    /// Credits points to a user's account.
    /// </summary>
    /// <param name="userId">The user to credit points to.</param>
    /// <param name="points">The number of points to credit.</param>
    /// <returns>The updated user entity.</returns>
    Task<User> CreditPointsAsync(Guid userId, int points);

    /// <summary>
    /// Reserves points for a pending transaction.
    /// </summary>
    /// <param name="userId">The user to reserve points for.</param>
    /// <param name="points">The number of points to reserve.</param>
    /// <returns>The updated user entity.</returns>
    Task<User> ReservePointsAsync(Guid userId, int points);

    /// <summary>
    /// Releases previously reserved points.
    /// </summary>
    /// <param name="userId">The user to release points for.</param>
    /// <param name="points">The number of points to release.</param>
    /// <returns>The updated user entity.</returns>
    Task<User> ReleasePointsAsync(Guid userId, int points);

    /// <summary>
    /// Captures previously reserved points (completes the transaction).
    /// </summary>
    /// <param name="userId">The user to capture points for.</param>
    /// <param name="points">The number of points to capture.</param>
    /// <returns>The updated user entity.</returns>
    Task<User> CapturePointsAsync(Guid userId, int points);

    /// <summary>
    /// Gets the current points balance for a user.
    /// </summary>
    /// <param name="userId">The user to get points for.</param>
    /// <returns>A tuple containing total points, locked points, and available points.</returns>
    Task<(int TotalPoints, int LockedPoints, int AvailablePoints)> GetPointsBalanceAsync(Guid userId);

    /// <summary>
    /// Validates if a user has sufficient available points.
    /// </summary>
    /// <param name="userId">The user to check.</param>
    /// <param name="requiredPoints">The number of points required.</param>
    /// <returns>True if the user has sufficient points, false otherwise.</returns>
    Task<bool> HasSufficientPointsAsync(Guid userId, int requiredPoints);
}

