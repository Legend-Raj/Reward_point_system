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
    private readonly IPointsService _pointsService;
    private readonly IUnitOfWork _unitOfWork;

    public RedemptionRequestService(
        IUserRepository userRepository,
        IProductRepository productRepository,
        IRedemptionRequestRepository redemptionRequestRepository,
        ILedgerEntryRepository ledgerEntryRepository,
        IPointsService pointsService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _redemptionRequestRepository = redemptionRequestRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _pointsService = pointsService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> RequestRedemptionAsync(Guid userId, Guid productId)
    {
        const int maxRetries = 3;
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var user = await _userRepository.GetUserByIdForUpdateAsync(userId)
                    ?? throw new DomainException(DomainErrors.User.NotFound);

                if (!user.IsActive)
                {
                    throw new DomainException(DomainErrors.User.AccountInactive);
                }

                var product = await _productRepository.GetProductByIdAsync(productId)
                    ?? throw new DomainException(DomainErrors.Product.NotFound);

                if (!product.IsActive)
                {
                    throw new DomainException(DomainErrors.Product.Inactive);
                }

                if (await _redemptionRequestRepository.HasPendingRedemptionRequestForProductAsync(userId, productId))
                {
                    throw new DomainException(DomainErrors.RedemptionRequest.AlreadyPending);
                }

                await _pointsService.ReservePointsAsync(user.Id, product.PointsCost);

                var redemptionRequest = RedemptionRequest.CreateNew(user.Id, product.Id);
                _redemptionRequestRepository.AddRedemptionRequest(redemptionRequest);

                await _unitOfWork.SaveChangesAsync();
                return redemptionRequest.Id;
            }
            catch (DomainException dex) when (attempt < maxRetries - 1 && dex.StatusCode == 409)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)));
                continue;
            }
            catch (Exception ex) when (attempt < maxRetries - 1 && IsConcurrencyException(ex))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)));
                continue;
            }
        }
        
        throw new DomainException("Unable to complete redemption request due to concurrent modifications. Please try again.", 409);
    }

    private static bool IsConcurrencyException(Exception ex)
    {
        var exceptionType = ex.GetType();
        var exceptionTypeName = exceptionType.Name;
        
        if (exceptionTypeName.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        if (exceptionTypeName.Contains("DbUpdate", StringComparison.OrdinalIgnoreCase))
        {
            var innerException = ex.InnerException;
            if (innerException != null)
            {
                var innerTypeName = innerException.GetType().Name;
                if (innerTypeName.Contains("SqlException", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    public async Task ApproveRedemptionAsync(Admin approver, Guid redemptionRequestId)
    {
        AdminGuard.EnsureActive(approver);

        var redemptionRequest = await _redemptionRequestRepository.GetRedemptionRequestByIdForUpdateAsync(redemptionRequestId)
            ?? throw new DomainException(DomainErrors.RedemptionRequest.NotFound);

        redemptionRequest.Approve();

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeliverRedemptionAsync(Admin deliverer, Guid redemptionRequestId)
    {
        AdminGuard.EnsureActive(deliverer);

        var redemptionRequest = await _redemptionRequestRepository.GetRedemptionRequestByIdForUpdateAsync(redemptionRequestId)
            ?? throw new DomainException(DomainErrors.RedemptionRequest.NotFound);

        var userAccount = await _userRepository.GetUserByIdForUpdateAsync(redemptionRequest.UserId)
            ?? throw new DomainException(DomainErrors.Repository.NonExistentUserForRedemption);

        var redeemedProduct = await _productRepository.GetProductByIdForUpdateAsync(redemptionRequest.ProductId)
            ?? throw new DomainException(DomainErrors.Repository.NonExistentProductForRedemption);

        if (!redeemedProduct.IsAvailableInStock())
        {
            throw new DomainException(DomainErrors.Product.InsufficientStock);
        }

        redemptionRequest.Deliver();

        await _pointsService.CapturePointsAsync(userAccount.Id, redeemedProduct.PointsCost);

        var ledgerEntry = new LedgerEntry(
            Guid.NewGuid(),
            userAccount.Id,
            LedgerEntryType.Redeem,
            redeemedProduct.PointsCost,
            DateTimeOffset.UtcNow,
            redemptionRequestId: redemptionRequest.Id);
        _ledgerEntryRepository.AddLedgerEntry(ledgerEntry);

        redeemedProduct.DecrementStock();

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RejectRedemptionAsync(Admin rejecter, Guid redemptionRequestId)
    {
        AdminGuard.EnsureActive(rejecter);

        var redemptionRequest = await _redemptionRequestRepository.GetRedemptionRequestByIdForUpdateAsync(redemptionRequestId)
            ?? throw new DomainException(DomainErrors.RedemptionRequest.NotFound);

        var user = await _userRepository.GetUserByIdForUpdateAsync(redemptionRequest.UserId)
            ?? throw new DomainException(DomainErrors.User.NotFound);

        var product = await _productRepository.GetProductByIdAsync(redemptionRequest.ProductId)
            ?? throw new DomainException(DomainErrors.Product.NotFound);

        redemptionRequest.Reject();
        await _pointsService.ReleasePointsAsync(user.Id, product.PointsCost);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelRedemptionAsync(Admin canceller, Guid redemptionRequestId)
    {
        AdminGuard.EnsureActive(canceller);

        var redemptionRequest = await _redemptionRequestRepository.GetRedemptionRequestByIdForUpdateAsync(redemptionRequestId)
            ?? throw new DomainException(DomainErrors.RedemptionRequest.NotFound);

        var user = await _userRepository.GetUserByIdForUpdateAsync(redemptionRequest.UserId)
            ?? throw new DomainException(DomainErrors.User.NotFound);

        var product = await _productRepository.GetProductByIdAsync(redemptionRequest.ProductId)
            ?? throw new DomainException(DomainErrors.Product.NotFound);

        redemptionRequest.Cancel();
        await _pointsService.ReleasePointsAsync(user.Id, product.PointsCost);

        await _unitOfWork.SaveChangesAsync();
    }

}

