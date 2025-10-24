using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

/// <summary>
/// Repository abstraction for persisting points ledger entries (previously named <c>IPointsTransactionRepository</c>).
/// </summary>
public interface ILedgerEntryRepository
{
    /// <summary>Persists a ledger entry representing a points movement.</summary>
    void AddLedgerEntry(LedgerEntry entry);

    /// <summary>Lists ledger entries recorded for a specific user.</summary>
    Task<IReadOnlyList<LedgerEntry>> ListLedgerEntriesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
