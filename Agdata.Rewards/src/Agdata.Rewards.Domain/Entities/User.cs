using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Domain.Entities;

public class User
{
    public Guid Id { get; }
    public Email Email { get; }
    public EmployeeId EmployeeId { get; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public int TotalPoints { get; private set; }
    public int LockedPoints { get; private set; }
    public int AvailablePoints => TotalPoints - LockedPoints;

    public User(Guid id, string name, Email email, EmployeeId employeeId, bool isActive = true, int totalPoints = 0, int lockedPoints = 0)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("User Id cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }
        if (totalPoints < 0 || lockedPoints < 0 || totalPoints < lockedPoints)
        {
            throw new DomainException("Invalid points state. Total points cannot be less than locked points.");
        }

        Id = id;
        Name = name.Trim();
        Email = email ?? throw new DomainException("Email is required.");
        EmployeeId = employeeId ?? throw new DomainException("EmployeeId is required.");
        IsActive = isActive;
        TotalPoints = totalPoints;
        LockedPoints = lockedPoints;
    }

    public static User CreateNew(string name, string email, string employeeId)
    {
        return new User(Guid.NewGuid(), name, new Email(email), new EmployeeId(employeeId));
    }

    public void ActivateAccount()
    {
        IsActive = true;
    }

    public void DeactivateAccount()
    {
        IsActive = false;
    }

    public void AddPoints(int points)
    {
        if (points <= 0)
        {
            throw new DomainException("Credit points must be a positive number.");
        }

        checked
        {
            TotalPoints += points;
        }
    }

    public void LockPoints(int pointsToLock)
    {
        if (pointsToLock <= 0)
        {
            throw new DomainException("Points to lock must be positive.");
        }
        if (AvailablePoints < pointsToLock)
        {
            throw new DomainException("Insufficient available points to lock.");
        }

        LockedPoints += pointsToLock;
    }

    public void UnlockPoints(int pointsToUnlock)
    {
        if (pointsToUnlock <= 0)
        {
            throw new DomainException("Points to unlock must be positive.");
        }
        if (LockedPoints < pointsToUnlock)
        {
            throw new DomainException("Cannot unlock more points than are locked.");
        }

        LockedPoints -= pointsToUnlock;
    }

    public void CommitLockedPoints(int pointsToCommit)
    {
        if (pointsToCommit <= 0)
        {
            throw new DomainException("Points to commit must be positive.");
        }
        if (LockedPoints < pointsToCommit)
        {
            throw new DomainException("Cannot commit more points than are locked.");
        }

        TotalPoints -= pointsToCommit;
        LockedPoints -= pointsToCommit;
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new DomainException("Name cannot be empty.");
        }

        Name = newName.Trim();
    }

    public override string ToString()
    {
        return $"{Name} ({Email}) [{EmployeeId}] - Total: {TotalPoints}, Available: {AvailablePoints}, Active: {IsActive}";
    }
}