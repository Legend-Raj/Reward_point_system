using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Application.Services;

public class ProductCatalogService : IProductCatalogService
{
    private readonly IProductRepository _productRepository;
    private readonly IRedemptionRepository _redemptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductCatalogService(
        IProductRepository productRepository,
        IRedemptionRepository redemptionRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _redemptionRepository = redemptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Product> CreateProductAsync(string name, int requiredPoints, int? stock, bool isActive = true)
    {
        var product = Product.CreateNew(name, requiredPoints, stock);

        if (!isActive)
        {
            product.MakeInactive();
        }

        _productRepository.Add(product);
        await _unitOfWork.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateProductAsync(Guid productId, string? name, int? requiredPoints, int? stock, bool? isActive)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new DomainException("Product not found.");

        if (!string.IsNullOrWhiteSpace(name))
        {
            product.ChangeName(name);
        }

        if (requiredPoints.HasValue)
        {
            product.UpdatePointsCost(requiredPoints.Value);
        }

        if (stock.HasValue)
        {
            product.UpdateStockQuantity(stock);
        }

        if (isActive.HasValue)
        {
            if (isActive.Value)
            {
                product.MakeActive();
            }
            else
            {
                product.MakeInactive();
            }
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    public async Task DeleteProductAsync(Guid productId)
    {
        if (await _redemptionRepository.AnyPendingRedemptionsForProductAsync(productId))
        {
            throw new DomainException("Product cannot be deleted as it has pending redemptions.");
        }

        _productRepository.Delete(productId);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Product>> GetCatalogAsync(bool onlyActive = true)
    {
        var allProducts = await _productRepository.GetAllAsync();
        var filtered = onlyActive
            ? allProducts.Where(product => product.IsActive)
            : allProducts;

        return filtered.ToList();
    }
}