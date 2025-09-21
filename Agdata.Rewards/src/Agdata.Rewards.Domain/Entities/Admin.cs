using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Domain.Entities;

/// <summary>
/// Admin = special User with elevated privileges.
/// NOTE:
/// - Business actions (allocate points, approve redemption, manage catalog)
///   entity ke andar NAHIN likhte — wo Application Services me hoti hain,
///   jahan authorization & orchestration handle hota hai.
/// - Admin ka yahan role sirf "type marker" ka hai (User se inherit karke).
/// </summary>
public sealed class Admin : User
{
    // Full-control constructor (infra/repository hydration)
    public Admin(Guid id, string name, Email email, EmployeeId employeeId, bool isActive = true, int pointsBalance = 0)
        : base(id, name, email, employeeId, isActive, pointsBalance)
    {
    }

    // Convenience factory for provisioning a NEW admin (fresh Guid)
    public static Admin CreateNew(string name, string email, string employeeId)
        => new(Guid.NewGuid(), name, new Email(email), new EmployeeId(employeeId));
}
