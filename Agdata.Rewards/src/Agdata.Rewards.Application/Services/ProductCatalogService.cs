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

    public async Task<Guid> AddNewProductAsync(Admin creator, string name, int requiredPoints, int? stock)
    {
        var product = Product.CreateNew(name, requiredPoints, stock);
        _productRepository.Add(product);
        await _unitOfWork.SaveChangesAsync();
        return product.Id;
    }

    public async Task UpdateProductDetailsAsync(Admin editor, Guid productId, string name, int requiredPoints, int? stock)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new DomainException("Product not found.");

        product.UpdateProductDetails(name, requiredPoints, stock);

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateProductAsync(Admin editor, Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new DomainException("Product not found.");

        product.MakeInactive();

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ActivateProductAsync(Admin editor, Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new DomainException("Product not found.");

        product.MakeActive();

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(Admin deleter, Guid productId)
    {
        if (await _redemptionRepository.AnyPendingRedemptionsForProductAsync(productId))
        {
            throw new DomainException("Product cannot be deleted as it has pending redemptions.");
        }

        _productRepository.Delete(productId);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<Product>> GetFullCatalogAsync()
    {
        return await _productRepository.GetAllAsync();
    }
}