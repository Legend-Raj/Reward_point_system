using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Application.Services.Shared;

internal static class AdminGuard
{
    private const int ForbiddenStatusCode = 403;

    public static void EnsureActive(Admin? admin, int statusCode = ForbiddenStatusCode)
    {
        if (admin is null)
        {
            throw new DomainException(DomainErrors.Authorization.AdminRequired, statusCode);
        }

        if (!admin.IsActive)
        {
            throw new DomainException(DomainErrors.Authorization.AdminInactive, statusCode);
        }
    }
}
