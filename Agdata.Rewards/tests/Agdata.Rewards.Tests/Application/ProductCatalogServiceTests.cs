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
    private static ProductCatalogService BuildService(ProductRepositoryInMemory productRepo, RedemptionRepositoryInMemory redemptionRepo)
        => new(productRepo, redemptionRepo, new InMemoryUnitOfWork());

    [Fact]
    public async Task CreateProductAsync_ShouldPersistProduct()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());

        var product = await service.CreateProductAsync("AGDATA Field Jacket", 1200, 20, true);

        var stored = await productRepo.GetByIdAsync(product.Id);
        Assert.NotNull(stored);
        Assert.Equal("AGDATA Field Jacket", stored!.Name);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task CreateProductAsync_WhenStockIsNull_ShouldSupportUnlimitedInventory()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());

        var product = await service.CreateProductAsync("AGDATA Legacy Pin", 75, null, true);

        Assert.Null(product.Stock);
        var persisted = await productRepo.GetByIdAsync(product.Id);
        Assert.NotNull(persisted);
        Assert.Null(persisted!.Stock);
    }

    [Fact]
    public async Task CreateProductAsync_WhenMarkedInactive_ShouldExcludeFromDefaultCatalog()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());
        var inactive = await service.CreateProductAsync("AGDATA Collector Edition", 3000, 1, false);
        var active = await service.CreateProductAsync("AGDATA Field Notebook", 90, null, true);

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
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Coffee Cup", 150, 10, true);

        var updated = await service.UpdateProductAsync(product.Id, "AGDATA Steel Tumbler", null, 5, false);

        Assert.Equal("AGDATA Steel Tumbler", updated.Name);
        Assert.Equal(150, updated.RequiredPoints);
        Assert.Equal(5, updated.Stock);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenReactivating_ShouldReturnProductToActiveCatalog()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Windbreaker", 400, 3, false);

        var reactivated = await service.UpdateProductAsync(product.Id, null, null, null, true);

        Assert.True(reactivated.IsActive);
        var catalog = await service.GetCatalogAsync();
        Assert.Contains(catalog, p => p.Id == product.Id);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenPendingRedemption_ShouldThrow()
    {
        var productRepo = new ProductRepositoryInMemory();
        var redemptionRepo = new RedemptionRepositoryInMemory();
        var service = BuildService(productRepo, redemptionRepo);
        var product = await service.CreateProductAsync("AGDATA Drone", 2500, 5, true);
        var pending = Redemption.CreateNew(Guid.NewGuid(), product.Id);
        redemptionRepo.Add(pending);

        await Assert.ThrowsAsync<DomainException>(() => service.DeleteProductAsync(product.Id));
    }

    [Fact]
    public async Task DeleteProductAsync_WhenNoPendingRedemptions_ShouldRemove()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());
        var product = await service.CreateProductAsync("AGDATA Hydro Bottle", 220, 5, true);

        await service.DeleteProductAsync(product.Id);

        Assert.Null(await productRepo.GetByIdAsync(product.Id));
    }

    [Fact]
    public async Task UpdateProductAsync_WhenMissing_ShouldThrow()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateProductAsync(Guid.NewGuid(), "AGDATA Hoodie", 400, 10, null));
    }

    [Fact]
    public async Task GetCatalogAsync_ShouldRespectOnlyActiveFilter()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());
        var active = await service.CreateProductAsync("AGDATA Soil Kit", 900, 15, true);
        var inactive = await service.CreateProductAsync("AGDATA Prototype", 1500, 2, false);

        var activeOnly = await service.GetCatalogAsync();
        Assert.Single(activeOnly);
        Assert.Equal(active.Id, activeOnly.First().Id);

        var all = await service.GetCatalogAsync(onlyActive: false);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, p => p.Id == inactive.Id);
    }
}
