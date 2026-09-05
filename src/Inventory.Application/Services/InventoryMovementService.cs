using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public class InventoryMovementService : IInventoryMovementService
{
    private const int MaxConcurrencyRetries = 3;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<Sale> _saleRepository;
    private readonly IInventoryMovementRepository _movementRepository;

    public InventoryMovementService(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        IRepository<Sale> saleRepository,
        IInventoryMovementRepository movementRepository)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _saleRepository = saleRepository;
        _movementRepository = movementRepository;
    }

    public Task<InventoryMovementResult> AdjustAsync(
        Guid userId, Guid productId, int quantityChange, string reason, CancellationToken cancellationToken = default) =>
        ApplyAsync(userId, productId, quantityChange, "Adjustment", reason, null, cancellationToken);

    public async Task<InventoryMovementResult> ReturnSaleAsync(
        Guid userId, Guid saleId, Guid productId, int quantity, string reason, CancellationToken cancellationToken = default)
    {
        if (quantity < 1)
            return new InventoryMovementResult(InventoryMovementOutcome.InvalidOperation);

        var sale = await _saleRepository.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
            return new InventoryMovementResult(InventoryMovementOutcome.SaleNotFound);

        var soldQuantity = sale.Lines.SingleOrDefault(line => line.ProductId == productId)?.Quantity ?? 0;
        var returnedQuantity = await _movementRepository.GetReturnedQuantityAsync(saleId, productId, cancellationToken);
        if (soldQuantity == 0 || returnedQuantity + quantity > soldQuantity)
            return new InventoryMovementResult(InventoryMovementOutcome.InvalidOperation);

        return await ApplyAsync(userId, productId, quantity, "CustomerReturn", reason, saleId, cancellationToken);
    }

    private async Task<InventoryMovementResult> ApplyAsync(
        Guid userId, Guid productId, int quantityChange, string type, string reason, Guid? saleId, CancellationToken cancellationToken)
    {
        if (quantityChange == 0 || string.IsNullOrWhiteSpace(reason))
            return new InventoryMovementResult(InventoryMovementOutcome.InvalidOperation);

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    var product = await _productRepository.GetByIdAsync(productId, ct);
                    if (product is null)
                        return new InventoryMovementResult(InventoryMovementOutcome.ProductNotFound);

                    if (product.Stock + quantityChange < 0)
                        return new InventoryMovementResult(InventoryMovementOutcome.InvalidOperation);

                    product.Stock += quantityChange;
                    await _productRepository.UpdateAsync(product, ct);

                    var movement = new InventoryMovement
                    {
                        Id = Guid.NewGuid(),
                        ProductId = productId,
                        SaleId = saleId,
                        PerformedByUserId = userId,
                        QuantityChange = quantityChange,
                        StockAfter = product.Stock,
                        Type = type,
                        Reason = reason.Trim()
                    };
                    await _movementRepository.AddAsync(movement, ct);
                    return new InventoryMovementResult(InventoryMovementOutcome.Success, movement);
                }, cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
            }
        }

        return new InventoryMovementResult(InventoryMovementOutcome.Conflict);
    }
}
