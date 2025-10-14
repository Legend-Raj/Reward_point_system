using System;
using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IRedemptionRequestService
{
    Task<Guid> RequestRedemptionAsync(Guid userId, Guid productId);
    Task ApproveRedemptionAsync(Admin approver, Guid redemptionRequestId);
    Task DeliverRedemptionAsync(Admin deliverer, Guid redemptionRequestId);
    Task RejectRedemptionAsync(Admin rejecter, Guid redemptionRequestId);
    Task CancelRedemptionAsync(Admin canceller, Guid redemptionRequestId);
}
