using System.Collections.Generic;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IPointsLedgerService
{
    Task<PointsTransaction> EarnAsync(Guid userId, Guid eventId, int points);
    Task<IReadOnlyList<PointsTransaction>> GetUserTransactionHistoryAsync(Guid userId);
    Task<Event> CreateEventAsync(string name, bool isActive = true);
    Task<IReadOnlyList<Event>> ListEventsAsync(bool onlyActive = true);
    Task<Event> SetEventActiveAsync(Guid eventId, bool isActive);
}