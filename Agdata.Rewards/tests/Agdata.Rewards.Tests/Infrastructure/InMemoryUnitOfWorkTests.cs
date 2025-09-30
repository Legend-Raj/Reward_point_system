using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Infrastructure.InMemory;
using Xunit;

namespace Agdata.Rewards.Tests.Infrastructure;

public class InMemoryUnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldCompleteImmediately()
    {
        var unitOfWork = new InMemoryUnitOfWork();

        var task = unitOfWork.SaveChangesAsync();

        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancelledToken_ShouldNotThrow()
    {
        var unitOfWork = new InMemoryUnitOfWork();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var task = unitOfWork.SaveChangesAsync(cts.Token);

        Assert.True(task.IsCompleted);
        await task;
    }
}
