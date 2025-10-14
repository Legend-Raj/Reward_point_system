using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Presentation.Api.Models.Requests;

public sealed class CreateUserRequest
{
    private const int MaxNameLength = 100;

    [Required(ErrorMessage = DomainErrors.PersonName.FirstRequired)]
    [StringLength(MaxNameLength, ErrorMessage = "First name is too long.")]
    public string FirstName { get; init; } = string.Empty;

    [StringLength(MaxNameLength, ErrorMessage = "Middle name is too long.")]
    public string? MiddleName { get; init; }

    [Required(ErrorMessage = DomainErrors.PersonName.LastRequired)]
    [StringLength(MaxNameLength, ErrorMessage = "Last name is too long.")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = DomainErrors.Email.Required)]
    [EmailAddress(ErrorMessage = DomainErrors.Email.InvalidFormat)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = DomainErrors.EmployeeId.Required)]
    [RegularExpression(@"^[A-Za-z]{3}-\d+$", ErrorMessage = DomainErrors.EmployeeId.InvalidFormat)]
    public string EmployeeId { get; init; } = string.Empty;

    public string NormalizeFirstName() => FirstName.Trim();

    public string? NormalizeMiddleName() => string.IsNullOrWhiteSpace(MiddleName) ? null : MiddleName.Trim();

    public string NormalizeLastName() => LastName.Trim();

    public string NormalizeEmail() => Email.Trim();

    public string NormalizeEmployeeId() => EmployeeId.Trim().ToUpperInvariant();
}

public sealed class UpdateUserRequest : IValidatableObject
{
    private const int MaxNameLength = 100;

    [StringLength(MaxNameLength, ErrorMessage = "First name is too long.")]
    public string? FirstName { get; init; }

    [StringLength(MaxNameLength, ErrorMessage = "Middle name is too long.")]
    public string? MiddleName { get; init; }

    [StringLength(MaxNameLength, ErrorMessage = "Last name is too long.")]
    public string? LastName { get; init; }

    [EmailAddress(ErrorMessage = DomainErrors.Email.InvalidFormat)]
    public string? Email { get; init; }

    [RegularExpression(@"^[A-Za-z]{3}-\d+$", ErrorMessage = DomainErrors.EmployeeId.InvalidFormat)]
    public string? EmployeeId { get; init; }

    public bool? IsActive { get; init; }

    public bool HasNameChange => FirstName is not null || MiddleName is not null || LastName is not null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FirstName is null && MiddleName is null && LastName is null && Email is null && EmployeeId is null && IsActive is null)
        {
            yield return new ValidationResult(
                "At least one field must be supplied for update.",
                new[] { nameof(FirstName), nameof(MiddleName), nameof(LastName), nameof(Email), nameof(EmployeeId), nameof(IsActive) });
        }

        if (FirstName is not null && string.IsNullOrWhiteSpace(FirstName))
        {
            yield return new ValidationResult(DomainErrors.PersonName.FirstRequired, new[] { nameof(FirstName) });
        }

        if (LastName is not null && string.IsNullOrWhiteSpace(LastName))
        {
            yield return new ValidationResult(DomainErrors.PersonName.LastRequired, new[] { nameof(LastName) });
        }

        if (Email is not null && string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult(DomainErrors.Validation.EmailRequired, new[] { nameof(Email) });
        }

        if (EmployeeId is not null && string.IsNullOrWhiteSpace(EmployeeId))
        {
            yield return new ValidationResult(DomainErrors.Validation.EmployeeIdRequired, new[] { nameof(EmployeeId) });
        }
    }

    public string? NormalizeFirstName() => FirstName?.Trim();

    public string? NormalizeMiddleName() => MiddleName is null
        ? null
        : (string.IsNullOrWhiteSpace(MiddleName) ? null : MiddleName.Trim());

    public string? NormalizeLastName() => LastName?.Trim();

    public string? NormalizeEmail() => Email?.Trim();

    public string? NormalizeEmployeeId() => EmployeeId?.Trim().ToUpperInvariant();
}
