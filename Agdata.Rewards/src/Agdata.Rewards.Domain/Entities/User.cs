using System;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Domain.Entities;

public class User
{
    public Guid Id { get; }
    public Email Email { get; private set; }
    public EmployeeId EmployeeId { get; private set; }
    public PersonName Name { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public int TotalPoints { get; private set; }
    public int LockedPoints { get; private set; }
    public int AvailablePoints => TotalPoints - LockedPoints;

    public User(
        Guid userId,
        PersonName name,
        Email email,
        EmployeeId employeeId,
        bool isActive = true,
        int totalPoints = 0,
        int lockedPoints = 0,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(DomainErrors.User.IdRequired);
        }
        if (totalPoints < 0 || lockedPoints < 0 || totalPoints < lockedPoints)
        {
            throw new DomainException(DomainErrors.User.InvalidPointsState);
        }

    Id = userId;
    Name = name ?? throw new DomainException(DomainErrors.User.NameRequired);
    Email = RequireEmail(email);
    EmployeeId = RequireEmployeeId(employeeId);
        IsActive = isActive;
        TotalPoints = totalPoints;
        LockedPoints = lockedPoints;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        if (effectiveCreatedAt == default)
        {
            effectiveCreatedAt = DateTimeOffset.UtcNow;
        }

        var effectiveUpdatedAt = updatedAt ?? effectiveCreatedAt;
        if (effectiveUpdatedAt == default)
        {
            effectiveUpdatedAt = effectiveCreatedAt;
        }

        if (effectiveUpdatedAt < effectiveCreatedAt)
        {
            throw new DomainException(DomainErrors.User.UpdatedBeforeCreated);
        }

        CreatedAt = effectiveCreatedAt;
        UpdatedAt = effectiveUpdatedAt;
    }

    public static User CreateNew(string firstName, string? middleName, string lastName, string email, string employeeId)
    {
        var now = DateTimeOffset.UtcNow;
        return new User(
            Guid.NewGuid(),
            PersonName.Create(firstName, middleName, lastName),
            new Email(email),
            new EmployeeId(employeeId),
            createdAt: now,
            updatedAt: now);
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch();
    }

    public void ChangeEmail(Email newEmail)
    {
        Email = RequireEmail(newEmail);
        Touch();
    }

    public void ChangeEmployeeId(EmployeeId newEmployeeId)
    {
        EmployeeId = RequireEmployeeId(newEmployeeId);
        Touch();
    }

    public void CreditPoints(int points)
    {
        if (points <= 0)
        {
            throw new DomainException(DomainErrors.User.CreditAmountMustBePositive);
        }

        checked
        {
            TotalPoints += points;
        }

        Touch();
    }

    public void ReservePoints(int pointsToLock)
    {
        if (pointsToLock <= 0)
        {
            throw new DomainException(DomainErrors.User.ReserveAmountMustBePositive);
        }
        if (AvailablePoints < pointsToLock)
        {
            throw new DomainException(DomainErrors.User.InsufficientPointsToReserve);
        }

        LockedPoints += pointsToLock;
        Touch();
    }

    public void ReleaseReservedPoints(int pointsToUnlock)
    {
        if (pointsToUnlock <= 0)
        {
            throw new DomainException(DomainErrors.User.ReleaseAmountMustBePositive);
        }
        if (LockedPoints < pointsToUnlock)
        {
            throw new DomainException(DomainErrors.User.ReleaseExceedsReserved);
        }

        LockedPoints -= pointsToUnlock;
        Touch();
    }

    public void CaptureReservedPoints(int pointsToCommit)
    {
        if (pointsToCommit <= 0)
        {
            throw new DomainException(DomainErrors.User.CaptureAmountMustBePositive);
        }
        if (LockedPoints < pointsToCommit)
        {
            throw new DomainException(DomainErrors.User.CaptureExceedsReserved);
        }

        TotalPoints -= pointsToCommit;
        LockedPoints -= pointsToCommit;
        Touch();
    }

    public void Rename(PersonName newName)
    {
        Name = newName ?? throw new DomainException(DomainErrors.User.NameRequired);
        Touch();
    }

    public override string ToString()
    {
        return $"{Name.FullName} ({Email}) [{EmployeeId}] - Total: {TotalPoints}, Available: {AvailablePoints}, Active: {IsActive}";
    }

    private void Touch()
    {
        var now = DateTimeOffset.UtcNow;
        UpdatedAt = now > UpdatedAt ? now : UpdatedAt.AddTicks(1);
    }

    private static Email RequireEmail(Email? email)
    {
        return email ?? throw new DomainException(DomainErrors.EmailRequired);
    }

    private static EmployeeId RequireEmployeeId(EmployeeId? employeeId)
    {
        return employeeId ?? throw new DomainException(DomainErrors.EmployeeIdRequired);
    }
}