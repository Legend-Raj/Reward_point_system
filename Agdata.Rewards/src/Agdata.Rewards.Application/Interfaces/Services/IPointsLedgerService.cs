using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IPointsLedgerService
{
    Task<Guid> AllocatePointsToUserForEventAsync(Admin allocator, Guid userId, Guid eventId, int points);
    Task<IEnumerable<PointsTransaction>> GetUserTransactionHistoryAsync(Guid userId);
}