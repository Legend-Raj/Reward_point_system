namespace Agdata.Rewards.Application.Interfaces.Auth;

public interface IAdminRegistry
{
    /// <summary>Determines whether the supplied identifiers map to a registered admin account.</summary>
    /// <param name="email">The admin's email address.</param>
    /// <param name="employeeId">The admin's employee identifier.</param>
    bool IsAdmin(string email, string employeeId);

    /// <summary>Registers an additional admin identifier such as an email or employee id.</summary>
    void AddAdminIdentifier(string adminIdentifier);

    /// <summary>Removes the supplied admin identifier from the registry.</summary>
    void RemoveAdminIdentifier(string adminIdentifier);
}