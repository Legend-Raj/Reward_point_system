using System;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

public class ProductCatalogServiceTests
{
    private static ProductCatalogService BuildService(ProductRepositoryInMemory productRepo, RedemptionRequestRepositoryInMemory redemptionRepo)
        => new(productRepo, redemptionRepo, new InMemoryUnitOfWork());

    [Fact]
    public async Task CreateProductAsync_ShouldPersistProduct()
    {
        var productRepo = new ProductRepositoryInMemory();
    var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());

        var product = await service.CreateProductAsync("AGDATA Field Jacket", "Premium insulated jacket", 1200, "https://example.com/jacket.png", 20, true);

        var stored = await productRepo.GetProductByIdAsync(product.Id);
        Assert.NotNull(stored);
        Assert.Equal("AGDATA Field Jacket", stored!.Name);
        Assert.Equal("Premium insulated jacket", stored.Description);
        Assert.Equal("https://example.com/jacket.png", stored.ImageUrl);
        Assert.Equal(1200, stored.PointsCost);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task CreateProductAsync_WhenStockIsNull_ShouldSupportUnlimitedInventory()
    {
        var productRepo = new ProductRepositoryInMemory();
    var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());

        var product = await service.CreateProductAsync("AGDATA Legacy Pin", null, 75, null, null, true);

        Assert.Null(product.Stock);
        var persisted = await productRepo.GetProductByIdAsync(product.Id);
        Assert.NotNull(persisted);
        Assert.Null(persisted!.Stock);
        Assert.Equal(75, persisted.PointsCost);
    }

    [Fact]
    public async Task CreateProductAsync_WhenMarkedInactive_ShouldExcludeFromDefaultCatalog()
    {
        var productRepo = new ProductRepositoryInMemory();
    var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());
        var inactive = await service.CreateProductAsync("AGDATA Collector Edition", "Limited run", 3000, null, 1, false);
        var active = await service.CreateProductAsync("AGDATA Field Notebook", null, 90, null, null, true);

        var defaultCatalog = await service.GetCatalogAsync();
        Assert.DoesNotContain(defaultCatalog, product => product.Id == inactive.Id);
        Assert.Contains(defaultCatalog, product => product.Id == active.Id);

        var fullCatalog = await service.GetCatalogAsync(onlyActive: false);
        Assert.Contains(fullCatalog, product => product.Id == inactive.Id);
        Assert.Contains(fullCatalog, product => product.Id == active.Id);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldApplyPartialChanges()
    {
        var productRepo = new ProductRepositoryInMemory();
    var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Coffee Cup", "Classic camp mug", 150, "https://example.com/mug.png", 10, true);

        var updated = await service.UpdateProductAsync(product.Id, "AGDATA Steel Tumbler", null, null, null, 5, false);

        Assert.Equal("AGDATA Steel Tumbler", updated.Name);
        Assert.Equal(150, updated.PointsCost);
        Assert.Equal(5, updated.Stock);
        Assert.Equal("Classic camp mug", updated.Description);
        Assert.Equal("https://example.com/mug.png", updated.ImageUrl);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenReactivating_ShouldReturnProductToActiveCatalog()
    {
        var productRepo = new ProductRepositoryInMemory();
    var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Windbreaker", "Wind resistant", 400, null, 3, false);

        var reactivated = await service.UpdateProductAsync(product.Id, null, null, null, null, null, true);

        Assert.True(reactivated.IsActive);
        var catalog = await service.GetCatalogAsync();
        Assert.Contains(catalog, p => p.Id == product.Id);
    }

    [Fact]
    public async Task SetStockQuantityAsync_ShouldPersistNewValue()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Field Hat", null, 150, null, 10, true);

        var updated = await service.SetStockQuantityAsync(product.Id, 25);

        Assert.Equal(25, updated.Stock);
        var persisted = await productRepo.GetProductByIdAsync(product.Id);
        Assert.Equal(25, persisted!.Stock);

        updated = await service.SetStockQuantityAsync(product.Id, null);
        Assert.Null(updated.Stock);
    }

    [Fact]
    public async Task IncrementStockAsync_ShouldIncreaseTrackedInventory()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Backpack", null, 500, null, 2, true);

        var updated = await service.IncrementStockAsync(product.Id, 3);

        Assert.Equal(5, updated.Stock);
        Assert.Equal(5, (await productRepo.GetProductByIdAsync(product.Id))!.Stock);
    }

    [Fact]
    public async Task DecrementStockAsync_ShouldReduceTrackedInventory()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Hoodie", null, 350, null, 4, true);

        var updated = await service.DecrementStockAsync(product.Id, 2);

        Assert.Equal(2, updated.Stock);
        Assert.Equal(2, (await productRepo.GetProductByIdAsync(product.Id))!.Stock);
    }

    [Fact]
    public async Task DecrementStockAsync_WhenInsufficient_ShouldThrow()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Premium Jacket", null, 800, null, 1, true);

        await Assert.ThrowsAsync<DomainException>(() => service.DecrementStockAsync(product.Id, 2));
    }

    [Fact]
    public async Task DeleteProductAsync_WhenPendingRedemption_ShouldThrow()
    {
    var productRepo = new ProductRepositoryInMemory();
    var redemptionRepo = new RedemptionRequestRepositoryInMemory();
        var service = BuildService(productRepo, redemptionRepo);
        var product = await service.CreateProductAsync("AGDATA Drone", null, 2500, null, 5, true);
        var pending = RedemptionRequest.CreateNew(Guid.NewGuid(), product.Id);
        redemptionRepo.AddRedemptionRequest(pending);

        await Assert.ThrowsAsync<DomainException>(() => service.DeleteProductAsync(product.Id));
    }

    [Fact]
    public async Task DeleteProductAsync_WhenNoPendingRedemptions_ShouldRemove()
    {
        var productRepo = new ProductRepositoryInMemory();
    var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Hydro Bottle", null, 220, null, 5, true);

        await service.DeleteProductAsync(product.Id);

        Assert.Null(await productRepo.GetProductByIdAsync(product.Id));
    }

    [Fact]
    public async Task UpdateProductAsync_WhenMissing_ShouldThrow()
    {
        var productRepo = new ProductRepositoryInMemory();
    var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateProductAsync(Guid.NewGuid(), "AGDATA Hoodie", null, 400, null, 10, null));
    }

    [Fact]
    public async Task GetCatalogAsync_ShouldRespectOnlyActiveFilter()
    {
        var productRepo = new ProductRepositoryInMemory();
    var service = BuildService(productRepo, new RedemptionRequestRepositoryInMemory());
        var active = await service.CreateProductAsync("AGDATA Soil Kit", null, 900, null, 15, true);
        var inactive = await service.CreateProductAsync("AGDATA Prototype", "Lab concept", 1500, null, 2, false);

        var activeOnly = await service.GetCatalogAsync();
        Assert.Single(activeOnly);
        Assert.Equal(active.Id, activeOnly.First().Id);

        var all = await service.GetCatalogAsync(onlyActive: false);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, p => p.Id == inactive.Id);
    }
}
