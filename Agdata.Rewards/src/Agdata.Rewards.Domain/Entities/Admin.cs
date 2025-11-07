using System;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Domain.Entities;

public sealed class Admin : User
{
    // Protected parameterless constructor for EF Core
    protected Admin() : base()
    {
        // EF Core only - do not use directly
    }

    public Admin(
        Guid adminId,
        PersonName name,
        Email email,
        EmployeeId employeeId,
        bool isActive = true,
        int totalPoints = 0,
        int lockedPoints = 0,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    : base(adminId, name, email, employeeId, isActive, totalPoints, lockedPoints, createdAt, updatedAt)
    {
        
    }

    public new static Admin CreateNew(string firstName, string? middleName, string lastName, string email, string employeeId)
    {
        var now = DateTimeOffset.UtcNow;
        return new Admin(
            Guid.NewGuid(),
            PersonName.Create(firstName, middleName, lastName),
            new Email(email),
            new EmployeeId(employeeId),
            createdAt: now,
            updatedAt: now);
    }
}