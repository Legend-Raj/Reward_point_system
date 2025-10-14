using System.Collections.Generic;

namespace Agdata.Rewards.Presentation.Api.Models.Responses;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Skip,
    int Take);
