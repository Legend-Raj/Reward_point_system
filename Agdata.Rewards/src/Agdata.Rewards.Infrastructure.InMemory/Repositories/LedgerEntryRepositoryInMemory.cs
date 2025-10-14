using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class LedgerEntryRepositoryInMemory : ILedgerEntryRepository
{
    private readonly List<LedgerEntry> _entries = new();
    private readonly object _gate = new();

    public void AddLedgerEntry(LedgerEntry entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);
        }
    }

    public Task<IReadOnlyList<LedgerEntry>> ListLedgerEntriesByUserAsync(Guid userId)
    {
        lock (_gate)
        {
            var slice = _entries
                .Where(entry => entry.UserId == userId)
                .OrderBy(entry => entry.Timestamp)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<LedgerEntry>>(slice);
        }
    }
}
