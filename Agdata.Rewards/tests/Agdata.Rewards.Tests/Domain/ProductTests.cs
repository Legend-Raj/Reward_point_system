using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void CreateNew_ShouldInitialiseStockAndCost()
    {
        var product = Product.CreateNew("Bluetooth Speaker", 1200, stock: 5);

        Assert.Equal("Bluetooth Speaker", product.Name);
        Assert.Equal(1200, product.RequiredPoints);
        Assert.Equal(5, product.Stock);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void DecrementStock_ShouldReduceTrackedStock()
    {
        var product = Product.CreateNew("Notebook", 200, stock: 3);

        product.DecrementStock();

        Assert.Equal(2, product.Stock);
    }

    [Fact]
    public void DecrementStock_WhenInsufficient_ShouldThrow()
    {
        var product = Product.CreateNew("Gift Card", 800, stock: 1);
        product.DecrementStock();

        Assert.Throws<DomainException>(() => product.DecrementStock());
    }

    [Fact]
    public void UpdateProductDetails_ShouldApplyValidChanges()
    {
        var product = Product.CreateNew("Cup", 100, stock: 10);

        product.UpdateProductDetails("Coffee Mug", 150, 8);

        Assert.Equal("Coffee Mug", product.Name);
        Assert.Equal(150, product.RequiredPoints);
        Assert.Equal(8, product.Stock);
    }

    [Fact]
    public void Validation_ShouldRejectInvalidInput()
    {
        Assert.Throws<DomainException>(() => Product.CreateNew(" ", 100));
        Assert.Throws<DomainException>(() => Product.CreateNew("Pen", 0));
        Assert.Throws<DomainException>(() => Product.CreateNew("Pen", 100, stock: -1));
    }
}
