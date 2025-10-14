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
    private readonly IRedemptionRequestRepository _redemptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductCatalogService(
        IProductRepository productRepository,
        IRedemptionRequestRepository redemptionRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _redemptionRepository = redemptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Product> CreateProductAsync(string name, string? description, int pointsCost, string? imageUrl, int? stock, bool isActive = true)
    {
        var product = Product.CreateNew(name, pointsCost, stock, description, imageUrl, isActive);

        _productRepository.AddProduct(product);
        await _unitOfWork.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateProductAsync(Guid productId, string? name, string? description, int? pointsCost, string? imageUrl, int? stock, bool? isActive)
    {
        var product = await _productRepository.GetProductByIdAsync(productId)
            ?? throw new DomainException(DomainErrors.Product.NotFound);

        var mergedName = name ?? product.Name;
        var mergedDescription = description ?? product.Description;
        var mergedPointsCost = pointsCost ?? product.PointsCost;
        var mergedImageUrl = imageUrl ?? product.ImageUrl;

    product.ApplyDetails(mergedName, mergedDescription, mergedPointsCost, mergedImageUrl);

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

        _productRepository.UpdateProduct(product);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    public async Task<Product> SetStockQuantityAsync(Guid productId, int? stock)
    {
        var product = await _productRepository.GetProductByIdAsync(productId)
            ?? throw new DomainException(DomainErrors.Product.NotFound);

        product.UpdateStockQuantity(stock);

        _productRepository.UpdateProduct(product);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    public async Task<Product> IncrementStockAsync(Guid productId, int quantity)
    {
        var product = await _productRepository.GetProductByIdAsync(productId)
            ?? throw new DomainException(DomainErrors.Product.NotFound);

        product.IncrementStock(quantity);

        _productRepository.UpdateProduct(product);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    public async Task<Product> DecrementStockAsync(Guid productId, int quantity)
    {
        var product = await _productRepository.GetProductByIdAsync(productId)
            ?? throw new DomainException(DomainErrors.Product.NotFound);

        product.DecrementStock(quantity);

        _productRepository.UpdateProduct(product);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    public async Task DeleteProductAsync(Guid productId)
    {
        if (await _redemptionRepository.AnyPendingRedemptionRequestsForProductAsync(productId))
        {
            throw new DomainException(DomainErrors.Product.CannotDeleteWithPending);
        }

        _productRepository.DeleteProduct(productId);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Product>> GetCatalogAsync(bool onlyActive = true)
    {
        var allProducts = await _productRepository.ListProductsAsync();
        var filtered = onlyActive
            ? allProducts.Where(product => product.IsActive)
            : allProducts;

        return filtered.ToList();
    }
}