using System;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int PointsCost { get; private set; }
    public string? ImageUrl { get; private set; }
    public int? Stock { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Product(
        Guid productId,
        string name,
        string? description,
        int pointsCost,
        string? imageUrl,
        int? stock = null,
        bool isActive = true,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException(DomainErrors.Product.IdRequired);
        }

        ValidateProductName(name);
        ValidatePointsCost(pointsCost);
        ValidateStockAmount(stock);

    Id = productId;
        Name = NormalizeRequired(name);
        Description = NormalizeOptional(description);
        PointsCost = pointsCost;
        ImageUrl = NormalizeOptional(imageUrl);
        Stock = stock;
        IsActive = isActive;

        var created = createdAt ?? DateTimeOffset.UtcNow;
        CreatedAt = created;
        UpdatedAt = DetermineInitialUpdatedAt(created, updatedAt);
    }

    public static Product CreateNew(
        string name,
        int pointsCost,
        int? stock = null,
        string? description = null,
        string? imageUrl = null,
        bool isActive = true)
    {
        return new Product(Guid.NewGuid(), name, description, pointsCost, imageUrl, stock, isActive);
    }

    public void MakeInactive()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch();
    }

    public void MakeActive()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
    }

    public void ApplyDetails(string name, string? description, int pointsCost, string? imageUrl)
    {
        ValidateProductName(name);
        ValidatePointsCost(pointsCost);

        Name = NormalizeRequired(name);
        Description = NormalizeOptional(description);
        PointsCost = pointsCost;
        ImageUrl = NormalizeOptional(imageUrl);

        Touch();
    }

    public void UpdateStockQuantity(int? stock)
    {
        ValidateStockAmount(stock);
        Stock = stock;
        Touch();
    }

    public bool IsAvailableInStock(int quantity = 1)
    {
        if (quantity <= 0)
        {
            throw new DomainException(DomainErrors.Product.QuantityMustBePositive);
        }

        return Stock is null || Stock.Value >= quantity;
    }

    public void DecrementStock(int quantity = 1)
    {
        if (quantity <= 0)
        {
            throw new DomainException(DomainErrors.Product.QuantityMustBePositive);
        }

        if (Stock is null)
        {
            return; // Untracked, do nothing
        }

        if (Stock.Value < quantity)
        {
            throw new DomainException(DomainErrors.Product.InsufficientStock);
        }

        Stock -= quantity;
        Touch();
    }

    public void IncrementStock(int quantity = 1)
    {
        if (quantity <= 0)
        {
            throw new DomainException(DomainErrors.Product.QuantityMustBePositive);
        }

        if (Stock is null)
        {
            return; // Untracked, do nothing
        }

        checked
        {
            Stock += quantity;
        }

        Touch();
    }

    private static void ValidateProductName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrors.Product.NameRequired);
        }
    }

    private static void ValidatePointsCost(int pointsCost)
    {
        if (pointsCost <= 0)
        {
            throw new DomainException(DomainErrors.Product.PointsCostPositive);
        }
    }

    private static void ValidateStockAmount(int? stock)
    {
        if (stock is < 0)
        {
            throw new DomainException(DomainErrors.Product.StockCannotBeNegative);
        }
    }

    private static string NormalizeRequired(string value) => value.Trim();

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static DateTimeOffset DetermineInitialUpdatedAt(DateTimeOffset createdAt, DateTimeOffset? updatedAt)
    {
        if (updatedAt is null)
        {
            return createdAt;
        }

        return updatedAt.Value >= createdAt ? updatedAt.Value : createdAt;
    }

    private void Touch(DateTimeOffset? timestamp = null)
    {
        var now = timestamp ?? DateTimeOffset.UtcNow;
        if (now <= UpdatedAt)
        {
            now = UpdatedAt.AddTicks(1);
        }

        UpdatedAt = now;
    }

    public override string ToString()
    {
        var stockDisplay = Stock?.ToString() ?? "unlimited";
        return $"{Name} (Cost: {PointsCost} pts, Stock: {stockDisplay}, Active: {IsActive})";
    }
}
