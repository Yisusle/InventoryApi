using System;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Services;

namespace Inventory.Application.Interfaces;

public interface IInventoryMovementService
{
    Task<InventoryMovementResult> AdjustAsync(
        Guid userId, Guid productId, int quantityChange, string reason, CancellationToken cancellationToken = default);

    Task<InventoryMovementResult> ReturnSaleAsync(
        Guid userId, Guid saleId, Guid productId, int quantity, string reason, CancellationToken cancellationToken = default);
}
