using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Agdata.Rewards.Infrastructure.SqlServer;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ILogger<EfUnitOfWork> _logger;

    public EfUnitOfWork(AppDbContext context, ILogger<EfUnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.FirstOrDefault();
            var entityType = entry?.Entity.GetType().Name ?? "Unknown";
            var entityId = entry?.Property("Id")?.CurrentValue?.ToString() ?? "Unknown";
            
            _logger.LogWarning(ex, 
                "Concurrency conflict detected. EntityType: {EntityType}, EntityId: {EntityId}",
                entityType, entityId);
            throw new DomainException("The record was modified by another operation. Please refresh and try again.", 409);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
        {
            if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
            {
                var entry = ex.Entries.FirstOrDefault();
                var entityType = entry?.Entity.GetType().Name ?? "Unknown";
                var entityId = entry?.Property("Id")?.CurrentValue?.ToString() ?? "Unknown";

                var msg = sqlEx.Message;
                var isEmailIndex = msg.Contains("IX_Users_Email_Value", StringComparison.OrdinalIgnoreCase);
                var isEmployeeIdIndex = msg.Contains("IX_Users_EmployeeId_Value", StringComparison.OrdinalIgnoreCase);
                
                var message = isEmailIndex
                    ? DomainErrors.User.EmailExists
                    : isEmployeeIdIndex
                        ? DomainErrors.User.EmployeeIdExists
                        : "A duplicate entry was detected.";

                _logger.LogWarning(ex, 
                    "Unique constraint violation. EntityType: {EntityType}, EntityId: {EntityId}, Message: {Message}",
                    entityType, entityId, message);
                throw new DomainException(message, 409);
            }

            var errorEntry = ex.Entries.FirstOrDefault();
            _logger.LogError(ex, 
                "Database update failed. EntityType: {EntityType}, EntityId: {EntityId}",
                errorEntry?.Entity.GetType().Name ?? "Unknown",
                errorEntry?.Property("Id")?.CurrentValue?.ToString() ?? "Unknown");
            throw new DomainException("An error occurred while saving changes.", 500);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL Server error: {ErrorNumber}", ex.Number);
            throw new DomainException("A database error occurred.", 500);
        }
    }
}

