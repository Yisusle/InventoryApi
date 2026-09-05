using System;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Services;

namespace Inventory.Application.Interfaces;

public interface IPurchaseService
{
    Task<PurchaseResult> CreatePurchaseAsync(
        Guid userId,
        Guid productId,
        int quantity,
        decimal totalCost,
        CancellationToken cancellationToken = default);
}
