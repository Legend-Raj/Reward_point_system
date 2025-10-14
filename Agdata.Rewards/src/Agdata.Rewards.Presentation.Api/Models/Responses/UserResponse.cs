using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Presentation.Api.Models.Responses;

public sealed record UserResponse(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string EmployeeId,
    bool IsActive,
    int TotalPoints,
    int LockedPoints,
    int AvailablePoints,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static UserResponse From(User user) => new(
        user.Id,
        user.Name.FirstName,
        user.Name.MiddleName,
        user.Name.LastName,
        user.Email.Value,
        user.EmployeeId.Value,
        user.IsActive,
        user.TotalPoints,
        user.LockedPoints,
        user.AvailablePoints,
        user.CreatedAt,
        user.UpdatedAt);
}
