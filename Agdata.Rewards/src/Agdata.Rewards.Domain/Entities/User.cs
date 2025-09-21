using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Domain.Entities;

/// <summary>
/// Ye User class hai jo AGDATA ke employee ko represent karta hai with points balance.
/// Basic rules hai ye:
/// - Points balance kabhi negative nahi ho sakta
/// - Credit/Debit amounts hamesha positive hona chahiye
/// - Inactive users ko points nahi milne chahiye (service level pe check karna hai),
///   lekin yahan bhi safety ke liye numeric checks hai
/// </summary>
public class User
{
    // Ye basic identity info hai jo change nahi hota
    public Guid Id { get; }
    public Email Email { get; }
    public EmployeeId EmployeeId { get; }

    // Profile aur current state info
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Abhi kitne points hai user ke paas - ye quick reading ke liye hai.
    /// Real history to PointsTransaction table mein milegi.
    /// </summary>
    public int PointsBalance { get; private set; }

    // Main constructor - ye use karo new user banane ke liye
    public User(Guid id, string name, Email email, EmployeeId employeeId, bool isActive = true, int pointsBalance = 0)
    {
        if (id == Guid.Empty) throw new DomainException("User Id cannot be empty.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (pointsBalance < 0) throw new DomainException("Points balance cannot be negative.");

        Id = id;
        Name = name.Trim();
        Email = email ?? throw new DomainException("Email is required.");
        EmployeeId = employeeId ?? throw new DomainException("EmployeeId is required.");
        IsActive = isActive;
        PointsBalance = pointsBalance;
    }

    /// <summary>
    /// Easy factory method - naya user banane ke liye use karo ye
    /// </summary>
    public static User CreateNew(string name, string email, string employeeId)
        => new(Guid.NewGuid(), name, new Email(email), new EmployeeId(employeeId));

    // Business logic aur rules yahan hai

    /// <summary> Employee ko activate karne ke liye </summary>
    public void Activate() => IsActive = true;

    /// <summary> Employee ko deactivate karne ke liye </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// User ke account mein points add karna hai to ye use karo.
    /// Points positive number hona chahiye.
    /// (Service layer ki responsibility hai ki sirf authorized calls hi aayein
    /// aur PointsTransaction table mein entry bhi kare)
    /// </summary>
    public void Credit(int points)
    {
        if (points <= 0)
            throw new DomainException("Credit points must be a positive number.");
        checked
        {
            PointsBalance += points;
        }
    }

    /// <summary>
    /// Points kam karne ke liye ye use karo. Amount positive hona chahiye aur balance se zyada nahi.
    /// </summary>
    public void Debit(int points)
    {
        if (points <= 0)
            throw new DomainException("Debit points must be a positive number.");
        if (PointsBalance < points)
            throw new DomainException("Insufficient balance.");
        PointsBalance -= points;
    }

    /// <summary>
    /// Agar user ka naam change karna hai to ye method use karo
    /// </summary>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Name cannot be empty.");
        Name = newName.Trim();
    }

    public override string ToString() => $"{Name} ({Email}) [{EmployeeId}] - Balance: {PointsBalance}, Active: {IsActive}";
}
