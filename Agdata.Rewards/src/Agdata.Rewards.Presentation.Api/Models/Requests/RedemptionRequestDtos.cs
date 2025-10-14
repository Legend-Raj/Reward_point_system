using System;

namespace Agdata.Rewards.Presentation.Api.Models.Requests;

public sealed record SubmitRedemptionRequestDto(Guid UserId, Guid ProductId);
