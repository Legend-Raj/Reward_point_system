using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class ProductCatalogServiceTests
{
    private static ProductCatalogService BuildService(ProductRepositoryInMemory productRepo, RedemptionRepositoryInMemory redemptionRepo)
        => new(productRepo, redemptionRepo, new InMemoryUnitOfWork());

    private static Admin CreateAdmin() => Admin.CreateNew("CatalogOwner", "catalog@example.com", "ADMIN-20");

    [Fact]
    public async Task AddNewProductAsync_ShouldPersistProduct()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());

        var productId = await service.AddNewProductAsync(CreateAdmin(), "T-Shirt", 800, 20);
        var stored = await productRepo.GetByIdAsync(productId);

        Assert.NotNull(stored);
        Assert.Equal("T-Shirt", stored!.Name);
    }

    [Fact]
    public async Task UpdateProductDetailsAsync_ShouldApplyChanges()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());
        var productId = await service.AddNewProductAsync(CreateAdmin(), "Cup", 100, 10);

        await service.UpdateProductDetailsAsync(CreateAdmin(), productId, "Steel Cup", 150, 5);
        var updated = await productRepo.GetByIdAsync(productId);

        Assert.Equal("Steel Cup", updated!.Name);
        Assert.Equal(150, updated.RequiredPoints);
        Assert.Equal(5, updated.Stock);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenPendingRedemption_ShouldThrow()
    {
        var productRepo = new ProductRepositoryInMemory();
        var redemptionRepo = new RedemptionRepositoryInMemory();
        var service = BuildService(productRepo, redemptionRepo);
        var productId = await service.AddNewProductAsync(CreateAdmin(), "Bag", 500, 5);
        var userId = Guid.NewGuid();
        var pending = Redemption.CreateNew(userId, productId);
        redemptionRepo.Add(pending);

        await Assert.ThrowsAsync<DomainException>(() => service.DeleteProductAsync(CreateAdmin(), productId));
    }

    [Fact]
    public async Task DeleteProductAsync_WhenNoPendingRedemptions_ShouldRemove()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());
        var productId = await service.AddNewProductAsync(CreateAdmin(), "Bottle", 200, 5);

        await service.DeleteProductAsync(CreateAdmin(), productId);
        var retrieved = await productRepo.GetByIdAsync(productId);

        Assert.Null(retrieved);
    }
}
