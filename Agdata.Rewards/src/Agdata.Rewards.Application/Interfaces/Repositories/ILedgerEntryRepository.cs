using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface ILedgerEntryRepository
{
    void AddLedgerEntry(LedgerEntry entry);
    Task<IReadOnlyList<LedgerEntry>> ListLedgerEntriesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LedgerEntry>> ListLedgerEntriesByUserAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountLedgerEntriesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
