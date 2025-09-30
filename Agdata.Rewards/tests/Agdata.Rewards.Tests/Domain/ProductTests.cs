using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void CreateNew_ShouldInitialiseStockAndCost()
    {
        var product = Product.CreateNew("AGDATA Field Tablet", 1200, stock: 5);

        Assert.Equal("AGDATA Field Tablet", product.Name);
        Assert.Equal(1200, product.RequiredPoints);
        Assert.Equal(5, product.Stock);
        Assert.True(product.IsActive);
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

        product.MakeActive();
        Assert.True(product.IsActive);
    }

    [Fact]
    public void UpdateProductDetails_ShouldApplyValidChanges()
    {
        var product = Product.CreateNew("Conference Mug", 100, stock: 10);

        product.UpdateProductDetails("AGDATA Coffee Mug", 150, 8);

        Assert.Equal("AGDATA Coffee Mug", product.Name);
        Assert.Equal(150, product.RequiredPoints);
        Assert.Equal(8, product.Stock);
    }

    [Fact]
    public void ChangeName_ShouldTrimAndPersist()
    {
        var product = Product.CreateNew("Stale Name", 300);

        product.ChangeName("  AGDATA Cooler  ");

        Assert.Equal("AGDATA Cooler", product.Name);
    }

    [Fact]
    public void UpdatePointsCost_ShouldRejectNonPositive()
    {
        var product = Product.CreateNew("Field Sensor", 900);

        Assert.Throws<DomainException>(() => product.UpdatePointsCost(0));
        Assert.Throws<DomainException>(() => product.UpdatePointsCost(-200));
    }

    [Fact]
    public void UpdateStockQuantity_ShouldValidateInput()
    {
        var product = Product.CreateNew("Yield Analyzer", 400, stock: 5);

        product.UpdateStockQuantity(10);
        Assert.Equal(10, product.Stock);

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
