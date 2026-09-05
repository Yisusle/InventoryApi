using System;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Domain.Entities;

namespace Inventory.Application.Interfaces;

public interface IInventoryMovementRepository : IRepository<InventoryMovement>
{
    Task<int> GetReturnedQuantityAsync(Guid saleId, Guid productId, CancellationToken cancellationToken = default);
}
