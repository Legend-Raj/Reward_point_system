using System;

namespace Agdata.Rewards.Domain.Exceptions;

public static class DomainErrors
{
    public static class Email
    {
        public const string Required = "Email is required.";
        public const string InvalidFormat = "Invalid email format.";
    }

    public static class EmployeeId
    {
        public const string Required = "EmployeeId is required.";
        public const string InvalidFormat = "EmployeeId format is invalid.";
    }

    public static class PersonName
    {
        public const string FirstRequired = "First name is required.";
        public const string LastRequired = "Last name is required.";
    }

    public static class User
    {
        public const string IdRequired = "User Id cannot be empty.";
        public const string NameRequired = "Name is required.";
        public const string InvalidPointsState = "Invalid points state. Total points cannot be less than locked points.";
        public const string UpdatedBeforeCreated = "Updated timestamp cannot be earlier than created timestamp.";
    public const string CreditAmountMustBePositive = "Credit points must be a positive number.";
    public const string ReserveAmountMustBePositive = "Points to lock must be positive.";
    public const string InsufficientPointsToReserve = "Insufficient available points to lock.";
    public const string ReleaseAmountMustBePositive = "Points to unlock must be positive.";
    public const string ReleaseExceedsReserved = "Cannot unlock more points than are locked.";
    public const string CaptureAmountMustBePositive = "Points to commit must be positive.";
    public const string CaptureExceedsReserved = "Cannot commit more points than are locked.";
        public const string EmailExists = "A user with this email already exists.";
        public const string EmployeeIdExists = "A user with this employee ID already exists.";
        public const string NotFound = "User not found.";
        public const string AccountInactive = "Inactive users cannot request redemptions.";
        public const string AllocationBlockedInactiveAccount = "Points cannot be allocated to an inactive user account.";
    }

    public static class Event
    {
        public const string IdRequired = "Event Id cannot be empty.";
        public const string NameRequired = "Event name is required.";
        public const string OccursAtRequired = "Event occurrence timestamp is required.";
        public const string NotFound = "Event not found.";
        public const string Inactive = "Points cannot be allocated for an inactive event.";
    }

    public static class Product
    {
        public const string IdRequired = "Product Id cannot be empty.";
        public const string NotFound = "Product not found.";
        public const string Inactive = "Cannot redeem an inactive product.";
        public const string InsufficientStock = "Insufficient stock.";
        public const string QuantityMustBePositive = "Quantity must be positive.";
        public const string StockCannotBeNegative = "Stock cannot be negative.";
    public const string PointsCostPositive = "Points cost must be a positive number.";
        public const string NameRequired = "Product name is required.";
        public const string PendingRedemption = "A pending redemption for this product already exists.";
        public const string CannotDeleteWithPending = "Product cannot be deleted as it has pending redemptions.";
    }

    public static class RedemptionRequest
    {
        public const string IdRequired = "Redemption request Id cannot be empty.";
        public const string UserRequired = "UserId is required for a redemption request.";
        public const string ProductRequired = "ProductId is required for a redemption request.";
        public const string NotFound = "Redemption request not found.";
        public const string AlreadyPending = "A pending redemption request for this product already exists.";
        public const string ApproveRequiresPending = "Only a 'Pending' redemption request can be approved.";
        public const string DeliverRequiresApproved = "Only an 'Approved' redemption request can be delivered.";
        public const string RejectRequiresPending = "Only a 'Pending' redemption request can be rejected.";
        public const string CancelRequiresPending = "Only a 'Pending' redemption request can be canceled.";
    }

    public static class Points
    {
        public const string MustBePositive = "Points must be a positive amount.";
        public const string CreditMustBePositive = "Credit points must be a positive number.";
        public const string TransactionMustBePositive = "Transaction points must be a positive number.";
        public const string EarnRequiresEvent = "'Earn' transaction must be linked to an event.";
    public const string RedeemRequiresRedemptionRequest = "'Redeem' transaction must be linked to a redemption request.";
        public const string TransactionIdRequired = "Transaction Id cannot be empty.";
        public const string UserRequired = "UserId is required for a transaction.";
    }

    public static class Authorization
    {
        public const string AdminRequired = "Only an administrator can perform this action.";
        public const string AdminInactive = "Administrator account is inactive.";
        public const string ForbiddenTitle = "Forbidden";
        public const string InvalidCredentials = "Invalid credentials provided.";
    }

    public static class Admin
    {
        public const string CannotRemoveLast = "Cannot remove the last admin from the system.";
    }

    public static class History
    {
        public const string UserMissing = "Cannot fetch history for a non-existent user.";
    }

    public static class Errors
    {
        public const string DomainViolationTitle = "Domain rule violated";
    }

    public static class Repository
    {
        public const string NonExistentUser = "Cannot allocate points to a non-existent user.";
        public const string NonExistentEvent = "Cannot allocate points for a non-existent event.";
        public const string NonExistentUserForHistory = "Cannot fetch history for a non-existent user.";
        public const string NonExistentUserForRedemption = "User associated with redemption not found.";
        public const string NonExistentProductForRedemption = "Product associated with redemption not found.";
    }

    public static class Validation
    {
        public const string FirstNameRequired = "First name is required.";
        public const string LastNameRequired = "Last name is required.";
        public const string EmailRequired = Email.Required;
        public const string EmployeeIdRequired = EmployeeId.Required;
        public const string SkipMustBeNonNegative = "Skip must be greater than or equal to zero.";
        public const string TakeMustBePositive = "Take must be greater than zero.";
        public const string TakeExceedsMaximum = "Take exceeds the maximum allowed page size.";
    }

    public const string EmailRequired = Email.Required;
    public const string InvalidEmailFormat = Email.InvalidFormat;
    public const string EmployeeIdRequired = EmployeeId.Required;
    public const string EmployeeIdInvalidFormat = EmployeeId.InvalidFormat;
}
