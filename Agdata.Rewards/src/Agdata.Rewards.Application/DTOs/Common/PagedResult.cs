using System.Collections.Generic;

namespace Agdata.Rewards.Application.DTOs.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Skip,
    int Take);
