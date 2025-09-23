using Agdata.Rewards.Application.Interfaces;

namespace Agdata.Rewards.Infrastructure.InMemory;

public class InMemoryUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}