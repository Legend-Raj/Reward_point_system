using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Presentation.Api.Models.Requests;

public sealed class CreateEventRequest : IValidatableObject
{
    [Required(ErrorMessage = DomainErrors.Authorization.AdminRequired)]
    public AdminActionDto? Admin { get; init; }

    [Required(ErrorMessage = DomainErrors.Event.NameRequired)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = DomainErrors.Event.OccursAtRequired)]
    public DateTimeOffset? OccursAt { get; init; }

    public bool IsActive { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Admin is null)
        {
            yield return new ValidationResult(DomainErrors.Authorization.AdminRequired, new[] { nameof(Admin) });
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(DomainErrors.Event.NameRequired, new[] { nameof(Name) });
        }

        if (OccursAt is null)
        {
            yield return new ValidationResult(DomainErrors.Event.OccursAtRequired, new[] { nameof(OccursAt) });
        }
    }

    public string NormalizeName() => Name.Trim();

    public DateTimeOffset NormalizeOccursAt() => OccursAt!.Value;
}

public sealed class UpdateEventRequest : IValidatableObject
{
    [Required(ErrorMessage = DomainErrors.Authorization.AdminRequired)]
    public AdminActionDto? Admin { get; init; }

    [Required(ErrorMessage = DomainErrors.Event.NameRequired)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = DomainErrors.Event.OccursAtRequired)]
    public DateTimeOffset? OccursAt { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Admin is null)
        {
            yield return new ValidationResult(DomainErrors.Authorization.AdminRequired, new[] { nameof(Admin) });
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(DomainErrors.Event.NameRequired, new[] { nameof(Name) });
        }

        if (OccursAt is null)
        {
            yield return new ValidationResult(DomainErrors.Event.OccursAtRequired, new[] { nameof(OccursAt) });
        }
    }

    public string NormalizeName() => Name.Trim();

    public DateTimeOffset NormalizeOccursAt() => OccursAt!.Value;
}
