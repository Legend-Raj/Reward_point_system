using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class PointsTransactionRepositoryInMemory : IPointsTransactionRepository
{
    private readonly List<PointsTransaction> _transactions = new();
    private readonly object _gate = new();

    public void Add(PointsTransaction transaction)
    {
        lock (_gate)
        {
            _transactions.Add(transaction);
        }
    }

    public Task<IReadOnlyList<PointsTransaction>> GetByUserIdAsync(Guid userId)
    {
        lock (_gate)
        {
            var slice = _transactions
                .Where(tx => tx.UserId == userId)
                .OrderBy(tx => tx.Timestamp)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<PointsTransaction>>(slice);
        }
    }
}