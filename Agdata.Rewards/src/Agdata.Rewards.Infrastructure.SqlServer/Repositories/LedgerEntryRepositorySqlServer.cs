using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Infrastructure.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Agdata.Rewards.Infrastructure.SqlServer.Repositories;

public class LedgerEntryRepositorySqlServer : ILedgerEntryRepository
{
    private readonly AppDbContext _context;

    public LedgerEntryRepositorySqlServer(AppDbContext context)
    {
        _context = context;
    }

    public void AddLedgerEntry(LedgerEntry entry)
    {
        _context.LedgerEntries.Add(entry);
    }

    public async Task<IReadOnlyList<LedgerEntry>> ListLedgerEntriesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await _context.LedgerEntries
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<IReadOnlyList<LedgerEntry>> ListLedgerEntriesByUserAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var list = await _context.LedgerEntries
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .ThenBy(l => l.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return list;
    }

    public Task<int> CountLedgerEntriesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.LedgerEntries
            .AsNoTracking()
            .CountAsync(l => l.UserId == userId, cancellationToken);
    }
}

