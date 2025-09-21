using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Domain.Entities;

/// <summary>
/// User = AGDATA employee with a points balance.
/// Domain invariants:
/// - PointsBalance >= 0
/// - Credit/Debit amounts must be > 0
/// - Inactive users shouldn't be allocated points (service-level guard),
///   but entity still protects its own numeric invariants.
/// </summary>
public class User
{
    // ----- Identity (immutable) -----
    public Guid Id { get; }
    public Email Email { get; }
    public EmployeeId EmployeeId { get; }

    // ----- Profile / State -----
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Current points snapshot for fast reads.
    /// True source-of-truth for history is the PointsTransaction ledger.
    /// </summary>
    public int PointsBalance { get; private set; }

    // ----- Constructors (use the main ctor or factory) -----
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
    /// Factory helper for convenience.
    /// </summary>
    public static User CreateNew(string name, string email, string employeeId)
        => new(Guid.NewGuid(), name, new Email(email), new EmployeeId(employeeId));

    // ----- Behavior / Rules -----

    /// <summary> Activate the employee account. </summary>
    public void Activate() => IsActive = true;

    /// <summary> Deactivate the employee account. </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Add points to user. Amount must be positive.
    /// (Service is responsible to ensure only authorized flows call this
    /// and also append a matching PointsTransaction ledger entry.)
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
    /// Deduct points from user. Amount must be positive and not exceed balance.
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
    /// Optional: rename user (kept small and safe).
    /// </summary>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Name cannot be empty.");
        Name = newName.Trim();
    }

    public override string ToString() => $"{Name} ({Email}) [{EmployeeId}] - Balance: {PointsBalance}, Active: {IsActive}";
}
