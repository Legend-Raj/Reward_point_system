using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class RedemptionTests
{
    [Fact]
    public void CreateNew_ShouldStartAsPending()
    {
        var redemption = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(RedemptionStatus.Pending, redemption.Status);
        Assert.NotEqual(default, redemption.RequestedAt);
        Assert.Null(redemption.ApprovedAt);
        Assert.Null(redemption.DeliveredAt);
    }

    [Fact]
    public void ApproveAndDeliver_ShouldFollowHappyPath()
    {
        var redemption = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());

        redemption.Approve();
        redemption.Deliver();

        Assert.Equal(RedemptionStatus.Delivered, redemption.Status);
        Assert.NotNull(redemption.ApprovedAt);
        Assert.NotNull(redemption.DeliveredAt);
    }

    [Fact]
    public void Approve_WhenNotPending_ShouldThrow()
    {
        var redemption = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        redemption.Approve();

        Assert.Throws<DomainException>(() => redemption.Approve());
    }

    [Fact]
    public void Deliver_WhenNotApproved_ShouldThrow()
    {
        var redemption = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<DomainException>(() => redemption.Deliver());
    }

    [Fact]
    public void RejectAndCancel_ShouldOnlyWorkFromPending()
    {
        var redemption = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        redemption.Reject();
        Assert.Equal(RedemptionStatus.Rejected, redemption.Status);

        var redemption2 = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        redemption2.Cancel();
        Assert.Equal(RedemptionStatus.Canceled, redemption2.Status);

        Assert.Throws<DomainException>(() => redemption.Reject());
        Assert.Throws<DomainException>(() => redemption2.Cancel());
    }

    [Fact]
    public void CreateNew_WithInvalidIdentifiers_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Redemption.CreateNew(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<DomainException>(() => Redemption.CreateNew(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Cancel_AfterApproval_ShouldThrow()
    {
        var redemption = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        redemption.Approve();

        Assert.Throws<DomainException>(() => redemption.Cancel());
    }

    [Fact]
    public void Deliver_AfterCancellationOrRejection_ShouldThrow()
    {
        var canceled = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        canceled.Cancel();

        var rejected = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        rejected.Reject();

        Assert.Throws<DomainException>(() => canceled.Deliver());
        Assert.Throws<DomainException>(() => rejected.Deliver());
    }

    [Fact]
    public void Approve_ShouldStampApprovedAtOnce()
    {
        var redemption = Redemption.CreateNew(Guid.NewGuid(), Guid.NewGuid());

        redemption.Approve();

        var firstApprovedAt = redemption.ApprovedAt;
        Assert.NotNull(firstApprovedAt);

        Assert.Throws<DomainException>(() => redemption.Approve());
        Assert.Equal(firstApprovedAt, redemption.ApprovedAt);
    }
}
