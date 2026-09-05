using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public class SaleService : ISaleService
{
    private const int MaxConcurrencyRetries = 3;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<InventoryMovement> _movementRepository;

    public SaleService(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        IRepository<Sale> saleRepository,
        IRepository<InventoryMovement> movementRepository)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _saleRepository = saleRepository;
        _movementRepository = movementRepository;
    }

    public async Task<SaleResult> CreateSaleAsync(
        Guid userId,
        IReadOnlyCollection<(Guid ProductId, int Quantity)> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0 || lines.Any(line => line.ProductId == Guid.Empty || line.Quantity < 1))
            return new SaleResult(SaleOutcome.InvalidSale);

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    var requestedQuantities = lines
                        .GroupBy(line => line.ProductId)
                        .Select(group => (ProductId: group.Key, Quantity: group.Sum(line => line.Quantity)))
                        .ToList();

                    var saleLines = new List<SaleLine>();
                    var products = new List<Product>();
                    foreach (var (productId, quantity) in requestedQuantities)
                    {
                        var product = await _productRepository.GetByIdAsync(productId, ct);
                        if (product is null)
                            return new SaleResult(SaleOutcome.ProductNotFound);

                        if (product.Stock < quantity)
                            return new SaleResult(SaleOutcome.InsufficientStock);

                        products.Add(product);
                        saleLines.Add(SaleLine.Create(product, quantity));
                    }

                    var sale = Sale.Create(userId, saleLines);

                    await _saleRepository.AddAsync(sale, ct);

                    foreach (var product in products)
                    {
                        var quantity = requestedQuantities.Single(line => line.ProductId == product.Id).Quantity;
                        product.Stock -= quantity;
                        await _productRepository.UpdateAsync(product, ct);
                        await _movementRepository.AddAsync(new InventoryMovement
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            SaleId = sale.Id,
                            PerformedByUserId = userId,
                            QuantityChange = -quantity,
                            StockAfter = product.Stock,
                            Type = "Sale",
                            Reason = "Venta registrada."
                        }, ct);
                    }

                    return new SaleResult(SaleOutcome.Success, sale);
                }, cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
            }
        }

        return new SaleResult(SaleOutcome.Conflict);
    }
}
