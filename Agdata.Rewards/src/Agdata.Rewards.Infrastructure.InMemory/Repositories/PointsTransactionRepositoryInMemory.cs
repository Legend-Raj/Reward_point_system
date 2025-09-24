using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class PointsTransactionRepositoryInMemory : IPointsTransactionRepository
{
    private readonly List<PointsTransaction> _transactions = new();

    public void Add(PointsTransaction transaction)
    {
        _transactions.Add(transaction);
    }
}