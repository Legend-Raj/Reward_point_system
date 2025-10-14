using System;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;

namespace Agdata.Rewards.Presentation.Api.Models.Responses;

public sealed record LedgerEntryResponse(
    Guid Id,
    Guid UserId,
    LedgerEntryType Type,
    int Points,
    DateTimeOffset Timestamp,
    Guid? EventId,
    Guid? RedemptionRequestId)
{
    public static LedgerEntryResponse From(LedgerEntry entry) => new(
        entry.Id,
        entry.UserId,
        entry.Type,
        entry.Points,
        entry.Timestamp,
        entry.EventId,
        entry.RedemptionRequestId);
}
