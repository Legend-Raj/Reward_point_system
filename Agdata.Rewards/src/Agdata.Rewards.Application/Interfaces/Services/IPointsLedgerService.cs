using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agdata.Rewards.Application.DTOs.Common;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IPointsLedgerService
{
    Task<LedgerEntry> EarnAsync(Admin actor, Guid userId, Guid eventId, int points);
    Task<PagedResult<LedgerEntry>> GetUserTransactionHistoryAsync(Guid userId, int skip, int take);
}