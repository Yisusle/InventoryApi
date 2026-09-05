using System;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public class PurchaseService : IPurchaseService
{
    private const int MaxConcurrencyRetries = 3;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<Purchase> _purchaseRepository;
    private readonly IRepository<InventoryMovement> _movementRepository;

    public PurchaseService(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        IRepository<Purchase> purchaseRepository,
        IRepository<InventoryMovement> movementRepository)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _purchaseRepository = purchaseRepository;
        _movementRepository = movementRepository;
    }

    public async Task<PurchaseResult> CreatePurchaseAsync(
        Guid userId,
        Guid productId,
        int quantity,
        decimal totalCost,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    var product = await _productRepository.GetByIdAsync(productId, ct);
                    if (product is null)
                        return new PurchaseResult(PurchaseOutcome.ProductNotFound);

                    var purchase = new Purchase
                    {
                        Id = Guid.NewGuid(),
                        CreatedByUserId = userId,
                        ProductId = productId,
                        Quantity = quantity,
                        TotalCost = totalCost
                    };

                    await _purchaseRepository.AddAsync(purchase, ct);

                    product.Stock += quantity;
                    await _productRepository.UpdateAsync(product, ct);
                    await _movementRepository.AddAsync(new InventoryMovement
                    {
                        Id = Guid.NewGuid(),
                        ProductId = productId,
                        PurchaseId = purchase.Id,
                        PerformedByUserId = userId,
                        QuantityChange = quantity,
                        StockAfter = product.Stock,
                        Type = "Purchase",
                        Reason = "Compra registrada."
                    }, ct);

                    return new PurchaseResult(PurchaseOutcome.Success, purchase);
                }, cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
            }
        }

        return new PurchaseResult(PurchaseOutcome.Conflict);
    }
}
