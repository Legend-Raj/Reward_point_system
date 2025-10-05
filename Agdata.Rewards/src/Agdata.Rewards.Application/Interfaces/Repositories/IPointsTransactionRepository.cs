using System;
using System.Collections.Generic;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IPointsTransactionRepository
{
    void Add(PointsTransaction transaction);
    Task<IReadOnlyList<PointsTransaction>> GetByUserIdAsync(Guid userId);
}