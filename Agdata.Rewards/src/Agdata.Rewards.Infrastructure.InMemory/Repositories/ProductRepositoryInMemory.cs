using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class ProductRepositoryInMemory : IProductRepository
{
    private readonly Dictionary<Guid, Product> _products = new();
    private readonly object _gate = new();

    public Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _products.TryGetValue(productId, out var product);
            return Task.FromResult(product);
        }
    }

    public Task<IEnumerable<Product>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_products.Values.ToList().AsEnumerable());
        }
    }

    public void AddProduct(Product product)
    {
        lock (_gate)
        {
            _products[product.Id] = product;
        }
    }

    public void UpdateProduct(Product product)
    {
        lock (_gate)
        {
            _products[product.Id] = product;
        }
    }

    public void DeleteProduct(Guid productId)
    {
        lock (_gate)
        {
            _products.Remove(productId);
        }
    }
}