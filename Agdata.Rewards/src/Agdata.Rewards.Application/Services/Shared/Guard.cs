using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Application.Services.Shared;

internal static class Guard
{
    public static void AgainstEmptyGuid(Guid value, string parameterName, string errorMessage)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(errorMessage);
        }
    }

    public static void AgainstDefaultDateTime(DateTimeOffset value, string errorMessage)
    {
        if (value == default)
        {
            throw new DomainException(errorMessage);
        }
    }

    public static void AgainstNegativeSkip(int skip)
    {
        if (skip < 0)
        {
            throw new DomainException(DomainErrors.Validation.SkipMustBeNonNegative);
        }
    }

    public static void AgainstInvalidTake(int take, int maxPageSize)
    {
        if (take <= 0)
        {
            throw new DomainException(DomainErrors.Validation.TakeMustBePositive);
        }

        if (take > maxPageSize)
        {
            throw new DomainException(DomainErrors.Validation.TakeExceedsMaximum);
        }
    }

    public static void AgainstNonPositive(int value, string errorMessage)
    {
        if (value <= 0)
        {
            throw new DomainException(errorMessage);
        }
    }
}

