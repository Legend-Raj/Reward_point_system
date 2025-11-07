using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Infrastructure.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Agdata.Rewards.Infrastructure.SqlServer.Repositories;

public class RedemptionRequestRepositorySqlServer : IRedemptionRequestRepository
{
    private readonly AppDbContext _context;

    public RedemptionRequestRepositorySqlServer(AppDbContext context)
    {
        _context = context;
    }

    public Task<RedemptionRequest?> GetRedemptionRequestByIdAsync(Guid redemptionRequestId, CancellationToken cancellationToken = default)
    {
        return _context.RedemptionRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == redemptionRequestId, cancellationToken);
    }

    public Task<RedemptionRequest?> GetRedemptionRequestByIdForUpdateAsync(Guid redemptionRequestId, CancellationToken cancellationToken = default)
    {
        return _context.RedemptionRequests
            .FirstOrDefaultAsync(r => r.Id == redemptionRequestId, cancellationToken);
    }

    public Task<bool> HasPendingRedemptionRequestForProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return _context.RedemptionRequests
            .AsNoTracking()
            .AnyAsync(r => 
                r.UserId == userId && 
                r.ProductId == productId && 
                r.Status == RedemptionRequestStatus.Pending, 
                cancellationToken);
    }

    public Task<bool> AnyPendingRedemptionRequestsForProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _context.RedemptionRequests
            .AsNoTracking()
            .AnyAsync(r => 
                r.ProductId == productId && 
                r.Status == RedemptionRequestStatus.Pending, 
                cancellationToken);
    }

    public void AddRedemptionRequest(RedemptionRequest redemptionRequest)
    {
        _context.RedemptionRequests.Add(redemptionRequest);
    }

    public void UpdateRedemptionRequest(RedemptionRequest redemptionRequest)
    {
        _context.RedemptionRequests.Update(redemptionRequest);
    }
}

