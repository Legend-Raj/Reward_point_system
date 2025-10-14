using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class ProductRepositoryInMemory : IProductRepository
{
    // Local dictionary mirrors keyed access a real datastore would offer while keeping in-memory tests fast.
    private readonly Dictionary<Guid, Product> _products = new();

    public Task<Product?> GetProductByIdAsync(Guid productId)
    {
        _products.TryGetValue(productId, out var product);
        return Task.FromResult(product);
    }

    public Task<IEnumerable<Product>> ListProductsAsync()
    {
        return Task.FromResult(_products.Values.AsEnumerable());
    }

    public void AddProduct(Product product)
    {
        _products[product.Id] = product;
    }

    public void UpdateProduct(Product product)
    {
        _products[product.Id] = product;
    }

    public void DeleteProduct(Guid productId)
    {
        _products.Remove(productId);
    }
}