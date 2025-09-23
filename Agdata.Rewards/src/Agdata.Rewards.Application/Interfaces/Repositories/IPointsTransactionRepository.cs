using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IPointsTransactionRepository
{
    void Add(PointsTransaction transaction);
}