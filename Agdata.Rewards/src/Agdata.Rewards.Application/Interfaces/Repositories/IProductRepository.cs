using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IProductRepository
{
    /// <summary>Fetches a product by its unique identifier.</summary>
    Task<Product?> GetProductByIdAsync(Guid productId);

    /// <summary>Lists the products available in the catalog.</summary>
    Task<IEnumerable<Product>> ListProductsAsync();

    /// <summary>Persists a newly created product.</summary>
    void AddProduct(Product product);

    /// <summary>Persists changes to an existing product.</summary>
    void UpdateProduct(Product product);

    /// <summary>Removes a product from the catalog.</summary>
    void DeleteProduct(Guid productId);
}