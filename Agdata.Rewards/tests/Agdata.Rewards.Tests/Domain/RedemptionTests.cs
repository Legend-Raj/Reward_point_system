using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class RedemptionRequestTests
{
    [Fact]
    public void CreateNew_ShouldStartAsPending()
    {
        var redemption = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(RedemptionRequestStatus.Pending, redemption.Status);
        Assert.NotEqual(default, redemption.RequestedAt);
        Assert.Null(redemption.ApprovedAt);
        Assert.Null(redemption.DeliveredAt);
    }

    [Fact]
    public void ApproveAndDeliver_ShouldFollowHappyPath()
    {
        var redemption = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());

        redemption.Approve();
        redemption.Deliver();

        Assert.Equal(RedemptionRequestStatus.Delivered, redemption.Status);
        Assert.NotNull(redemption.ApprovedAt);
        Assert.NotNull(redemption.DeliveredAt);
    }

    [Fact]
    public void Approve_WhenNotPending_ShouldThrow()
    {
        var redemption = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        redemption.Approve();

        Assert.Throws<DomainException>(() => redemption.Approve());
    }

    [Fact]
    public void Deliver_WhenNotApproved_ShouldThrow()
    {
        var redemption = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<DomainException>(() => redemption.Deliver());
    }

    [Fact]
    public void RejectAndCancel_ShouldOnlyWorkFromPending()
    {
        var redemption = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        redemption.Reject();
        Assert.Equal(RedemptionRequestStatus.Rejected, redemption.Status);

        var redemption2 = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        redemption2.Cancel();
        Assert.Equal(RedemptionRequestStatus.Canceled, redemption2.Status);

        Assert.Throws<DomainException>(() => redemption.Reject());
        Assert.Throws<DomainException>(() => redemption2.Cancel());
    }

    [Fact]
    public void CreateNew_WithInvalidIdentifiers_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => RedemptionRequest.CreateNew(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<DomainException>(() => RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Cancel_AfterApproval_ShouldThrow()
    {
        var redemption = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        redemption.Approve();

        Assert.Throws<DomainException>(() => redemption.Cancel());
    }

    [Fact]
    public void Deliver_AfterCancellationOrRejection_ShouldThrow()
    {
        var canceled = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        canceled.Cancel();

        var rejected = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());
        rejected.Reject();

        Assert.Throws<DomainException>(() => canceled.Deliver());
        Assert.Throws<DomainException>(() => rejected.Deliver());
    }

    [Fact]
    public void Approve_ShouldStampApprovedAtOnce()
    {
        var redemption = RedemptionRequest.CreateNew(Guid.NewGuid(), Guid.NewGuid());

        redemption.Approve();

        var firstApprovedAt = redemption.ApprovedAt;
        Assert.NotNull(firstApprovedAt);

        Assert.Throws<DomainException>(() => redemption.Approve());
        Assert.Equal(firstApprovedAt, redemption.ApprovedAt);
    }
}
