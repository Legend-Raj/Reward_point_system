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

    private static Admin CreateAdmin() => Admin.CreateNew("Naomi Fields", "naomi.fields@agdata.com", "AGD-ADMIN-220");

    [Fact]
    public async Task AddNewProductAsync_ShouldPersistProduct()
    {
    var productRepo = new ProductRepositoryInMemory();
    var service = BuildService(productRepo, new RedemptionRepositoryInMemory());

    var productId = await service.AddNewProductAsync(CreateAdmin(), "AGDATA Field Jacket", 1200, 20);
        var stored = await productRepo.GetByIdAsync(productId);

        Assert.NotNull(stored);
    Assert.Equal("AGDATA Field Jacket", stored!.Name);
    }

    [Fact]
    public async Task UpdateProductDetailsAsync_ShouldApplyChanges()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());
        var productId = await service.AddNewProductAsync(CreateAdmin(), "AGDATA Coffee Cup", 150, 10);

        await service.UpdateProductDetailsAsync(CreateAdmin(), productId, "AGDATA Steel Tumbler", 180, 5);
        var updated = await productRepo.GetByIdAsync(productId);

        Assert.Equal("AGDATA Steel Tumbler", updated!.Name);
        Assert.Equal(180, updated.RequiredPoints);
        Assert.Equal(5, updated.Stock);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenPendingRedemption_ShouldThrow()
    {
        var productRepo = new ProductRepositoryInMemory();
        var redemptionRepo = new RedemptionRepositoryInMemory();
    var service = BuildService(productRepo, redemptionRepo);
    var productId = await service.AddNewProductAsync(CreateAdmin(), "AGDATA Drone", 2500, 5);
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
        var productId = await service.AddNewProductAsync(CreateAdmin(), "AGDATA Hydro Bottle", 220, 5);

        await service.DeleteProductAsync(CreateAdmin(), productId);
        var retrieved = await productRepo.GetByIdAsync(productId);

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task UpdateProductDetailsAsync_WhenMissing_ShouldThrow()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());

        await Assert.ThrowsAsync<DomainException>(() => service.UpdateProductDetailsAsync(CreateAdmin(), Guid.NewGuid(), "AGDATA Hoodie", 400, 10));
    }

    [Fact]
    public async Task ActivateAndDeactivateProduct_ShouldToggleState()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());
        var productId = await service.AddNewProductAsync(CreateAdmin(), "AGDATA Soil Kit", 900, 15);

        await service.DeactivateProductAsync(CreateAdmin(), productId);
        Assert.False((await productRepo.GetByIdAsync(productId))!.IsActive);

        await service.ActivateProductAsync(CreateAdmin(), productId);
        Assert.True((await productRepo.GetByIdAsync(productId))!.IsActive);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenMissing_ShouldNoOp()
    {
        var productRepo = new ProductRepositoryInMemory();
        var service = BuildService(productRepo, new RedemptionRepositoryInMemory());

        var missingProductId = Guid.NewGuid();

        await service.DeleteProductAsync(CreateAdmin(), missingProductId);

        Assert.Null(await productRepo.GetByIdAsync(missingProductId));
    }
}
