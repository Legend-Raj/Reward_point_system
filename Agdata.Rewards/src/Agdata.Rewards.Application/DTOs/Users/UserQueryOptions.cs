namespace Agdata.Rewards.Application.DTOs.Users;

public sealed record UserQueryOptions(
    int Skip = 0,
    int Take = 25,
    bool? IsActive = null,
    string? Search = null
)
{
    public static UserQueryOptions Default { get; } = new();
}
