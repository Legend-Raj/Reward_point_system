using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public int RequiredPoints { get; private set; }
    public int? Stock { get; private set; }
    public bool IsActive { get; private set; }

    public Product(Guid id, string name, int requiredPoints, int? stock = null, bool isActive = true)
    {
        if (id == Guid.Empty) 
        {
            throw new DomainException("Product Id cannot be empty.");
        }
        
        ValidateProductName(name);
        ValidatePointsRequired(requiredPoints);
        ValidateStockAmount(stock);

        Id = id;
        Name = name.Trim();
        RequiredPoints = requiredPoints;
        Stock = stock;
        IsActive = isActive;
    }

    public static Product CreateNewProduct(string name, int requiredPoints, int? stock = null)
    {
        return new Product(Guid.NewGuid(), name, requiredPoints, stock);
    }

    public void MakeInactive() 
    {
        IsActive = false;
    }

    public void MakeActive() 
    {
        IsActive = true;
    }

    public void UpdateProductDetails(string name, int requiredPoints, int? stock)
    {
        ValidateProductName(name);
        ValidatePointsRequired(requiredPoints);
        ValidateStockAmount(stock);

        Name = name.Trim();
        RequiredPoints = requiredPoints;
        Stock = stock;
    }

    public void ChangeName(string name)
    {
        ValidateProductName(name);
        Name = name.Trim();
    }

    public void UpdatePointsCost(int requiredPoints)
    {
        ValidatePointsRequired(requiredPoints);
        RequiredPoints = requiredPoints;
    }

    public void UpdateStockQuantity(int? stock)
    {
        ValidateStockAmount(stock);
        Stock = stock;
    }

    public bool IsAvailableInStock(int quantity = 1)
    {
        if (quantity <= 0) 
        {
            throw new DomainException("Quantity must be positive.");
        }
        
        return Stock is null || Stock.Value >= quantity;
    }

    public void ReduceStock(int quantity = 1)
    {
        if (quantity <= 0) 
        {
            throw new DomainException("Quantity must be positive.");
        }
        
        if (Stock is null) 
        {
            return;
        }

        if (Stock.Value < quantity)
            throw new DomainException("Insufficient stock.");

        Stock -= quantity;
    }

    public void RestoreStock(int quantity = 1)
    {
        if (quantity <= 0) 
        {
            throw new DomainException("Quantity must be positive.");
        }
        
        if (Stock is null) 
        {
            return;
        }

        checked 
        { 
            Stock += quantity; 
        }
    }

    private static void ValidateProductName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name is required.");
        }
    }

    private static void ValidatePointsRequired(int requiredPoints)
    {
        if (requiredPoints <= 0)
            throw new DomainException("Required points must be a positive number.");
    }

    private static void ValidateStockAmount(int? stock)
    {
        if (stock is < 0)
            throw new DomainException("Stock cannot be negative.");
    }

    public override string ToString()
    {
        var stockDisplay = Stock?.ToString() ?? "unlimited";
        return $"{Name} (Cost: {RequiredPoints} pts, Stock: {stockDisplay}, Active: {IsActive})";
    }
}
