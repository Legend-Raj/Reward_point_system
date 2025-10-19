using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IProductCatalogService
{
    Task<Product> CreateProductAsync(Admin admin, string name, string? description, int pointsCost, string? imageUrl, int? stock, bool isActive = true);
    Task<Product> UpdateProductAsync(Admin admin, Guid productId, string? name, string? description, int? pointsCost, string? imageUrl, int? stock, bool? isActive);
    Task<Product> SetStockQuantityAsync(Admin admin, Guid productId, int? stock);
    Task<Product> IncrementStockAsync(Admin admin, Guid productId, int quantity);
    Task<Product> DecrementStockAsync(Admin admin, Guid productId, int quantity);
    Task DeleteProductAsync(Admin admin, Guid productId);
    Task<IReadOnlyList<Product>> GetCatalogAsync(bool onlyActive = true);
}