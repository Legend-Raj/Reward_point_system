using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Domain.Entities;

public sealed class Admin : User
{
    public Admin(Guid id, string name, Email email, EmployeeId employeeId, bool isActive = true, int pointsBalance = 0)
        : base(id, name, email, employeeId, isActive, pointsBalance)
    {
    }

    public static Admin CreateNewAdmin(string name, string email, string employeeId)
    {
        return new Admin(Guid.NewGuid(), name, new Email(email), new EmployeeId(employeeId));
    }
}
