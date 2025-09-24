using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Infrastructure.InMemory.Repositories;

public class ProductRepositoryInMemory : IProductRepository
{
    private readonly Dictionary<Guid, Product> _products = new();

    public Task<Product?> GetByIdAsync(Guid id)
    {
        _products.TryGetValue(id, out var product);
        return Task.FromResult(product);
    }

    public Task<IEnumerable<Product>> GetAllAsync()
    {
        return Task.FromResult(_products.Values.AsEnumerable());
    }

    public void Add(Product product)
    {
        _products[product.Id] = product;
    }

    public void Update(Product product)
    {
        _products[product.Id] = product;
    }

    public void Delete(Guid id)
    {
        _products.Remove(id);
    }
}