using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Agdata.Rewards.Infrastructure.SqlServer.Repositories;

public class ProductRepositorySqlServer : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepositorySqlServer(AppDbContext context)
    {
        _context = context;
    }

    public Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }

    public Task<Product?> GetProductByIdForUpdateAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }

    public async Task<IEnumerable<Product>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public void AddProduct(Product product)
    {
        _context.Products.Add(product);
    }

    public void UpdateProduct(Product product)
    {
        _context.Products.Update(product);
    }

    public void DeleteProduct(Guid productId)
    {
        var product = _context.Products.Find(productId);
        if (product == null)
        {
            throw new DomainException(DomainErrors.Product.NotFound, 404);
        }
        
        _context.Products.Remove(product);
    }
}

