using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Services;

public interface IRedemptionService
{
    Task<Guid> RequestRedemptionAsync(Guid userId, Guid productId);
    Task ApproveRedemptionAsync(Admin approver, Guid redemptionId);
    Task DeliverRedemptionAsync(Admin deliverer, Guid redemptionId);
    Task RejectRedemptionAsync(Admin rejecter, Guid redemptionId);
    Task CancelRedemptionAsync(Admin canceller, Guid redemptionId);
}