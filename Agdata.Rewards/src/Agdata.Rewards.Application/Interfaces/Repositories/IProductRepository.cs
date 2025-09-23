using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
}