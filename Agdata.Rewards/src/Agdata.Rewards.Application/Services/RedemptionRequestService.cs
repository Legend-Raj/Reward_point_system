using System;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Application.Services.Shared;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Application.Services;

public class RedemptionRequestService : IRedemptionRequestService
{
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRedemptionRequestRepository _redemptionRequestRepository;
    private readonly ILedgerEntryRepository _ledgerEntryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RedemptionRequestService(
        IUserRepository userRepository,
        IProductRepository productRepository,
        IRedemptionRequestRepository redemptionRequestRepository,
        ILedgerEntryRepository ledgerEntryRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _redemptionRequestRepository = redemptionRequestRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> RequestRedemptionAsync(Guid userId, Guid productId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId)
            ?? throw new DomainException(DomainErrors.User.NotFound);

        var product = await _productRepository.GetProductByIdAsync(productId)
            ?? throw new DomainException(DomainErrors.Product.NotFound);

        if (!user.IsActive)
        {
            throw new DomainException(DomainErrors.User.AccountInactive);
        }

        if (!product.IsActive)
        {
            throw new DomainException(DomainErrors.Product.Inactive);
        }

        if (await _redemptionRequestRepository.HasPendingRedemptionRequestForProductAsync(userId, productId))
        {
            throw new DomainException(DomainErrors.RedemptionRequest.AlreadyPending);
        }

        user.ReservePoints(product.PointsCost);

        var redemptionRequest = RedemptionRequest.CreateNew(user.Id, product.Id);

        _redemptionRequestRepository.AddRedemptionRequest(redemptionRequest);
        _userRepository.UpdateUser(user);

        await _unitOfWork.SaveChangesAsync();

        return redemptionRequest.Id;
    }

    public async Task ApproveRedemptionAsync(Admin approver, Guid redemptionRequestId)
    {
        AdminGuard.EnsureActive(approver);

        var redemptionRequest = await _redemptionRequestRepository.GetRedemptionRequestByIdAsync(redemptionRequestId)
            ?? throw new DomainException(DomainErrors.RedemptionRequest.NotFound);

        redemptionRequest.Approve();

        _redemptionRequestRepository.UpdateRedemptionRequest(redemptionRequest);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeliverRedemptionAsync(Admin deliverer, Guid redemptionRequestId)
    {
        AdminGuard.EnsureActive(deliverer);

        var redemptionRequest = await _redemptionRequestRepository.GetRedemptionRequestByIdAsync(redemptionRequestId)
            ?? throw new DomainException(DomainErrors.RedemptionRequest.NotFound);

        var userAccount = await _userRepository.GetUserByIdAsync(redemptionRequest.UserId)
            ?? throw new DomainException(DomainErrors.Repository.NonExistentUserForRedemption);

        var redeemedProduct = await _productRepository.GetProductByIdAsync(redemptionRequest.ProductId)
            ?? throw new DomainException(DomainErrors.Repository.NonExistentProductForRedemption);

        if (!redeemedProduct.IsAvailableInStock())
        {
            throw new DomainException(DomainErrors.Product.InsufficientStock);
        }

        redemptionRequest.Deliver();

        userAccount.CaptureReservedPoints(redeemedProduct.PointsCost);

        var ledgerEntry = new LedgerEntry(
            Guid.NewGuid(),
            userAccount.Id,
            LedgerEntryType.Redeem,
            redeemedProduct.PointsCost,
            DateTimeOffset.UtcNow,
            redemptionRequestId: redemptionRequest.Id);
        _ledgerEntryRepository.AddLedgerEntry(ledgerEntry);

        redeemedProduct.DecrementStock();

        _userRepository.UpdateUser(userAccount);
        _productRepository.UpdateProduct(redeemedProduct);
        _redemptionRequestRepository.UpdateRedemptionRequest(redemptionRequest);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RejectRedemptionAsync(Admin rejecter, Guid redemptionRequestId)
    {
        AdminGuard.EnsureActive(rejecter);

        var redemptionRequest = await _redemptionRequestRepository.GetRedemptionRequestByIdAsync(redemptionRequestId)
            ?? throw new DomainException(DomainErrors.RedemptionRequest.NotFound);

        var user = await _userRepository.GetUserByIdAsync(redemptionRequest.UserId)
            ?? throw new DomainException(DomainErrors.User.NotFound);

        var product = await _productRepository.GetProductByIdAsync(redemptionRequest.ProductId)
            ?? throw new DomainException(DomainErrors.Product.NotFound);

        redemptionRequest.Reject();
        user.ReleaseReservedPoints(product.PointsCost);

        _userRepository.UpdateUser(user);
        _redemptionRequestRepository.UpdateRedemptionRequest(redemptionRequest);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelRedemptionAsync(Admin canceller, Guid redemptionRequestId)
    {
        AdminGuard.EnsureActive(canceller);

        var redemptionRequest = await _redemptionRequestRepository.GetRedemptionRequestByIdAsync(redemptionRequestId)
            ?? throw new DomainException(DomainErrors.RedemptionRequest.NotFound);

        var user = await _userRepository.GetUserByIdAsync(redemptionRequest.UserId)
            ?? throw new DomainException(DomainErrors.User.NotFound);

        var product = await _productRepository.GetProductByIdAsync(redemptionRequest.ProductId)
            ?? throw new DomainException(DomainErrors.Product.NotFound);

        redemptionRequest.Cancel();
        user.ReleaseReservedPoints(product.PointsCost);

        _userRepository.UpdateUser(user);
        _redemptionRequestRepository.UpdateRedemptionRequest(redemptionRequest);

        await _unitOfWork.SaveChangesAsync();
    }

}

