using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IProductCatalogService
{
    Task<Product> CreateProductAsync(string name, string? description, int pointsCost, string? imageUrl, int? stock, bool isActive = true);
    Task<Product> UpdateProductAsync(Guid productId, string? name, string? description, int? pointsCost, string? imageUrl, int? stock, bool? isActive);
    Task<Product> SetStockQuantityAsync(Guid productId, int? stock);
    Task<Product> IncrementStockAsync(Guid productId, int quantity);
    Task<Product> DecrementStockAsync(Guid productId, int quantity);
    Task DeleteProductAsync(Guid productId);
    Task<IReadOnlyList<Product>> GetCatalogAsync(bool onlyActive = true);
}