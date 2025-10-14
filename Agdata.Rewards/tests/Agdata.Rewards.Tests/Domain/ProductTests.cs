using System;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void CreateNew_ShouldInitialiseCosmeticFieldsAndAudit()
    {
        var before = DateTimeOffset.UtcNow;
        var product = Product.CreateNew("AGDATA Field Tablet", 1200, stock: 5, description: "Tablet bundle", imageUrl: " https://example.com/tablet.png ");

        Assert.Equal("AGDATA Field Tablet", product.Name);
        Assert.Equal("Tablet bundle", product.Description);
        Assert.Equal("https://example.com/tablet.png", product.ImageUrl);
        Assert.Equal(1200, product.PointsCost);
        Assert.Equal(5, product.Stock);
        Assert.True(product.IsActive);
        Assert.InRange(product.CreatedAt, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Equal(product.CreatedAt, product.UpdatedAt);
    }

    [Fact]
    public void CreateNew_WithOptionalStock_ShouldAllowNullStock()
    {
    var productWithStock = Product.CreateNew("Soil Notebook", 200, stock: 3);
    var productWithoutStock = Product.CreateNew("Notebook", 200);

        Assert.Equal(3, productWithStock.Stock);
        Assert.Null(productWithoutStock.Stock);
    }

    [Fact]
    public void MakeInactive_ShouldToggleFlag()
    {
        var product = Product.CreateNew("Dealer Gift Card", 800, stock: 1);

        product.MakeInactive();
        Assert.False(product.IsActive);
        Assert.True(product.UpdatedAt > product.CreatedAt);

        product.MakeActive();
        Assert.True(product.IsActive);
        Assert.True(product.UpdatedAt > product.CreatedAt);
    }

    [Fact]
    public void ApplyDetails_ShouldApplyValidChangesAndBumpAudit()
    {
        var product = Product.CreateNew("Conference Mug", 100, stock: 10, description: "Ceramic", imageUrl: "http://example.com/mug.png");
        var before = product.UpdatedAt;

    product.ApplyDetails(" AGDATA Coffee Mug ", "  New ceramic finish  ", 150, " http://example.com/mug-new.png ");

        Assert.Equal("AGDATA Coffee Mug", product.Name);
        Assert.Equal("New ceramic finish", product.Description);
        Assert.Equal(150, product.PointsCost);
        Assert.Equal("http://example.com/mug-new.png", product.ImageUrl);
        Assert.True(product.UpdatedAt > before);
    }

    [Fact]
    public void ApplyDetails_ShouldRejectInvalidName()
    {
        var product = Product.CreateNew("Stale Name", 300);

    Assert.Throws<DomainException>(() => product.ApplyDetails(" ", null, 300, null));
    }

    [Fact]
    public void ApplyDetails_ShouldRejectNonPositivePointsCost()
    {
        var product = Product.CreateNew("Field Sensor", 900);

    Assert.Throws<DomainException>(() => product.ApplyDetails("Field Sensor", null, 0, null));
    Assert.Throws<DomainException>(() => product.ApplyDetails("Field Sensor", null, -200, null));
    }

    [Fact]
    public void UpdateStockQuantity_ShouldValidateInput()
    {
        var product = Product.CreateNew("Yield Analyzer", 400, stock: 5);
        var before = product.UpdatedAt;

        product.UpdateStockQuantity(10);
        Assert.Equal(10, product.Stock);
        Assert.True(product.UpdatedAt > before);

        Assert.Throws<DomainException>(() => product.UpdateStockQuantity(-1));
    }

    [Fact]
    public void IsAvailableInStock_ShouldHandleUnlimitedInventory()
    {
        var unlimitedProduct = Product.CreateNew("Digital Subscription", 1500);
        var trackedProduct = Product.CreateNew("Field Jacket", 700, stock: 2);

    Assert.True(unlimitedProduct.IsAvailableInStock(100));
    Assert.True(trackedProduct.IsAvailableInStock(2));
    Assert.False(trackedProduct.IsAvailableInStock(3));
        Assert.Throws<DomainException>(() => trackedProduct.IsAvailableInStock(0));
    }

    [Fact]
    public void DecrementStock_ShouldRespectBoundaries()
    {
        var product = Product.CreateNew("Soil Probe", 650, stock: 2);

    product.DecrementStock();
    Assert.Equal(1, product.Stock);

        Assert.Throws<DomainException>(() => product.DecrementStock(0));
        Assert.Throws<DomainException>(() => product.DecrementStock(-1));
        Assert.Throws<DomainException>(() => product.DecrementStock(5));

        var unlimited = Product.CreateNew("Training Credit", 500);
        unlimited.DecrementStock();
        Assert.Null(unlimited.Stock);
    }

    [Fact]
    public void IncrementStock_ShouldIncreaseTrackedInventory()
    {
        var product = Product.CreateNew("Weather Station", 1800, stock: 1);

    product.IncrementStock();
    Assert.Equal(2, product.Stock);

        Assert.Throws<DomainException>(() => product.IncrementStock(0));
        Assert.Throws<DomainException>(() => product.IncrementStock(-2));

        var unlimited = Product.CreateNew("Consulting Hours", 2200);
        unlimited.IncrementStock();
        Assert.Null(unlimited.Stock);
    }

    [Fact]
    public void Validation_ShouldRejectInvalidInput()
    {
        Assert.Throws<DomainException>(() => Product.CreateNew(" ", 100));
        Assert.Throws<DomainException>(() => Product.CreateNew("Promo Pen", 0));
        Assert.Throws<DomainException>(() => Product.CreateNew("Promo Pen", 100, stock: -1));
    }
}
