using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Domain.Entities;

public sealed class Admin : User
{
    public Admin(Guid id, string name, Email email, EmployeeId employeeId, bool isActive = true, int totalPoints = 0, int lockedPoints = 0)
        : base(id, name, email, employeeId, isActive, totalPoints, lockedPoints)
    {
        // Admin inherits all base properties and behavior from User
    }

    public new static Admin CreateNew(string name, string email, string employeeId)
    {
        return new Admin(Guid.NewGuid(), name, new Email(email), new EmployeeId(employeeId));
    }
}