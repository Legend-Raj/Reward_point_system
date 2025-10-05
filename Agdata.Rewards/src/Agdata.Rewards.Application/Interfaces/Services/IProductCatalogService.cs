using System.Collections.Generic;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IProductCatalogService
{
    Task<Product> CreateProductAsync(string name, int requiredPoints, int? stock, bool isActive = true);
    Task<Product> UpdateProductAsync(Guid productId, string? name, int? requiredPoints, int? stock, bool? isActive);
    Task DeleteProductAsync(Guid productId);
    Task<IReadOnlyList<Product>> GetCatalogAsync(bool onlyActive = true);
}