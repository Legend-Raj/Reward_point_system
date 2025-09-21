using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Domain.Entities;

/// <summary>
/// Product matlab ye wo items hai jo users apne points se redeem kar sakte hain.
/// Basic rules ye hai:
/// - Name khali nahi hona chahiye
/// - RequiredPoints hamesha positive hona chahiye
/// - Stock: null matlab unlimited, warna >= 0 hona chahiye
/// - Business logic services mein handle hogi, yahan sirf basic validation hai
/// </summary>
public sealed class Product
{
    // Product ki basic identity
    public Guid Id { get; }

    // Product ki details aur state
    public string Name { get; private set; }

    /// <summary>Redeem karne ke liye kitne points lagenge (positive number hona chahiye)</summary>
    public int RequiredPoints { get; private set; }

    /// <summary>
    /// Stock management:
    /// - null matlab unlimited stock hai
    /// - number hai to remaining units (negative nahi ho sakta)
    /// </summary>
    public int? Stock { get; private set; }

    /// <summary>Sirf active products ko redeem kar sakte hain</summary>
    public bool IsActive { get; private set; }

    // Constructor aur factory methods
    public Product(Guid id, string name, int requiredPoints, int? stock = null, bool isActive = true)
    {
        if (id == Guid.Empty) throw new DomainException("Product Id cannot be empty.");
        ValidateName(name);
        ValidateRequiredPoints(requiredPoints);
        ValidateStock(stock);

        Id = id;
        Name = name.Trim();
        RequiredPoints = requiredPoints;
        Stock = stock;          // null means unlimited stock
        IsActive = isActive;
    }

    public static Product CreateNew(string name, int requiredPoints, int? stock = null)
        => new(Guid.NewGuid(), name, requiredPoints, stock);

    // Business logic aur behavior methods

    /// <summary>Product ko temporarily band kar dena</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Product ko wapas chalu kar dena</summary>
    public void Activate() => IsActive = true;

    /// <summary>Admin ke liye - name, points aur stock sab ek saath update karna</summary>
    public void Update(string name, int requiredPoints, int? stock)
    {
        ValidateName(name);
        ValidateRequiredPoints(requiredPoints);
        ValidateStock(stock);

        Name = name.Trim();
        RequiredPoints = requiredPoints;
        Stock = stock;
    }

    /// <summary>Sirf product ka naam change karna hai to ye use karo</summary>
    public void Rename(string name)
    {
        ValidateName(name);
        Name = name.Trim();
    }

    /// <summary>Sirf required points change karne ke liye</summary>
    public void ChangeRequiredPoints(int requiredPoints)
    {
        ValidateRequiredPoints(requiredPoints);
        RequiredPoints = requiredPoints;
    }

    /// <summary>Stock set karne ke liye (null means unlimited)</summary>
    public void SetStock(int? stock)
    {
        ValidateStock(stock);
        Stock = stock;
    }

    /// <summary>
    /// Check karna hai ki requested quantity available hai ya nahi?
    /// Unlimited stock (null) ho to hamesha true return karega.
    /// </summary>
    public bool HasStock(int quantity = 1)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        return Stock is null || Stock.Value >= quantity;
    }

    /// <summary>
    /// Stock kam karne ke liye - agar stock tracking on hai to quantity minus kar dega.
    /// Unlimited stock (null) mein kuch nahi hoga.
    /// Redemption approve karte time ye method call karna hai.
    /// </summary>
    public void DecrementStockIfTracked(int quantity = 1)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        if (Stock is null) return; // unlimited stock, nothing to decrease

        if (Stock.Value < quantity)
            throw new DomainException("Insufficient stock.");

        Stock -= quantity;
    }

    /// <summary>
    /// Stock wapas badhane ke liye - return ya cancellation ke time useful hai.
    /// Unlimited stock (null) mein kuch nahi hoga.
    /// </summary>
    public void IncrementStockIfTracked(int quantity = 1)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        if (Stock is null) return; // unlimited stock, nothing to increase

        checked { Stock += quantity; }
    }

    // Validation methods - data ko validate karne ke liye
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");
    }

    private static void ValidateRequiredPoints(int requiredPoints)
    {
        if (requiredPoints <= 0)
            throw new DomainException("Required points must be a positive number.");
    }

    private static void ValidateStock(int? stock)
    {
        if (stock is < 0)
            throw new DomainException("Stock cannot be negative.");
    }

    public override string ToString()
        => $"{Name} (Cost: {RequiredPoints} pts, Stock: {(Stock?.ToString() ?? "unlimited")}, Active: {IsActive})";
}
