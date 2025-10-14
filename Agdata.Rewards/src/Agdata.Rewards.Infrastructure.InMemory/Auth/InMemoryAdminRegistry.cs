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

    public void AddAdminIdentifier(string adminIdentifier)
    {
        _adminIdentifiers.Add(adminIdentifier);
    }

    public void RemoveAdminIdentifier(string adminIdentifier)
    {
        if (_adminIdentifiers.Count <= 1)
        {
            throw new DomainException(DomainErrors.Admin.CannotRemoveLast);
        }
        _adminIdentifiers.Remove(adminIdentifier);
    }
}