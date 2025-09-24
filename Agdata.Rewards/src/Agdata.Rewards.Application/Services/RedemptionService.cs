using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.Exceptions;

namespace Agdata.Rewards.Application.Services;

public class RedemptionService : IRedemptionService
{
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IRedemptionRepository _redemptionRepository;
    private readonly IPointsTransactionRepository _pointsTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RedemptionService(
        IUserRepository userRepository,
        IProductRepository productRepository,
        IRedemptionRepository redemptionRepository,
        IPointsTransactionRepository pointsTransactionRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _redemptionRepository = redemptionRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> RequestRedemptionAsync(Guid userId, Guid productId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new DomainException("User not found.");

        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new DomainException("Product not found.");

        if (!user.IsActive)
        {
            throw new DomainException("Inactive users cannot request redemptions.");
        }

        if (!product.IsActive)
        {
            throw new DomainException("Cannot redeem an inactive product.");
        }

        if (await _redemptionRepository.HasPendingRedemptionForProductAsync(userId, productId))
        {
            throw new DomainException("A pending redemption for this product already exists.");
        }

        user.LockPoints(product.RequiredPoints);

        var redemption = Redemption.CreateNew(user.Id, product.Id);

        _redemptionRepository.Add(redemption);
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync();

        return redemption.Id;
    }

    public async Task ApproveRedemptionAsync(Admin approver, Guid redemptionId)
    {
        var redemption = await _redemptionRepository.GetByIdAsync(redemptionId)
            ?? throw new DomainException("Redemption not found.");

        redemption.Approve();

        _redemptionRepository.Update(redemption);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeliverRedemptionAsync(Admin deliverer, Guid redemptionId)
    {
        var redemption = await _redemptionRepository.GetByIdAsync(redemptionId)
            ?? throw new DomainException("Redemption not found.");

        var userAccount = await _userRepository.GetByIdAsync(redemption.UserId)
            ?? throw new DomainException("User associated with redemption not found.");

        var redeemedProduct = await _productRepository.GetByIdAsync(redemption.ProductId)
            ?? throw new DomainException("Product associated with redemption not found.");

        userAccount.CommitLockedPoints(redeemedProduct.RequiredPoints);

        var pointsTransaction = new PointsTransaction(
            Guid.NewGuid(),
            userAccount.Id,
            TransactionType.Redeem,
            redeemedProduct.RequiredPoints,
            DateTimeOffset.UtcNow,
            redemptionId: redemption.Id
        );
        _pointsTransactionRepository.Add(pointsTransaction);

        redeemedProduct.DecrementStock();

        redemption.Deliver();

        _userRepository.Update(userAccount);
        _productRepository.Update(redeemedProduct);
        _redemptionRepository.Update(redemption);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RejectRedemptionAsync(Admin rejecter, Guid redemptionId)
    {
        var redemption = await _redemptionRepository.GetByIdAsync(redemptionId)
            ?? throw new DomainException("Redemption not found.");

        var user = await _userRepository.GetByIdAsync(redemption.UserId)
            ?? throw new DomainException("User not found.");

        var product = await _productRepository.GetByIdAsync(redemption.ProductId)
            ?? throw new DomainException("Product not found.");

        user.UnlockPoints(product.RequiredPoints);
        redemption.Reject();

        _userRepository.Update(user);
        _redemptionRepository.Update(redemption);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelRedemptionAsync(Admin canceller, Guid redemptionId)
    {
        var redemption = await _redemptionRepository.GetByIdAsync(redemptionId)
            ?? throw new DomainException("Redemption not found.");

        var user = await _userRepository.GetByIdAsync(redemption.UserId)
            ?? throw new DomainException("User not found.");

        var product = await _productRepository.GetByIdAsync(redemption.ProductId)
            ?? throw new DomainException("Product not found.");

        user.UnlockPoints(product.RequiredPoints);
        redemption.Cancel();

        _userRepository.Update(user);
        _redemptionRepository.Update(redemption);

        await _unitOfWork.SaveChangesAsync();
    }
}