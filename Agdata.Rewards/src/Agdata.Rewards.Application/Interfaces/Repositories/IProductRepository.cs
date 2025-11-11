using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<Product?> GetProductByIdForUpdateAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> ListProductsAsync(CancellationToken cancellationToken = default);
    void AddProduct(Product product);
    void UpdateProduct(Product product);
    void DeleteProduct(Guid productId);
}