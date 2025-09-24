using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Infrastructure.InMemory.Auth;

public class InMemoryAdminRegistry : IAdminRegistry
{
    private readonly HashSet<string> _adminIdentifiers;

    public InMemoryAdminRegistry(IEnumerable<string> initialAdminIdentifiers)
    {
        _adminIdentifiers = new HashSet<string>(initialAdminIdentifiers, StringComparer.OrdinalIgnoreCase);
        if (_adminIdentifiers.Count == 0)
        {
            throw new InvalidOperationException("System must be seeded with at least one admin.");
        }
    }

    public bool IsAdmin(string email, string employeeId)
    {
        return _adminIdentifiers.Contains(email) || _adminIdentifiers.Contains(employeeId);
    }

    public void AddAdmin(string emailOrEmployeeId)
    {
        _adminIdentifiers.Add(emailOrEmployeeId);
    }

    public void RemoveAdmin(string emailOrEmployeeId)
    {
        if (_adminIdentifiers.Count <= 1)
        {
            throw new DomainException("Cannot remove the last admin from the system.");
        }
        _adminIdentifiers.Remove(emailOrEmployeeId);
    }
}