using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public class InventoryMovementRepository : IInventoryMovementRepository
{
    private readonly AppDbContext _db;

    public InventoryMovementRepository(AppDbContext db) => _db = db;

    public Task<InventoryMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.InventoryMovements.FindAsync(new object[] { id }, cancellationToken).AsTask();

    public async Task<(IEnumerable<InventoryMovement> Items, int TotalCount)> ListPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .Include(m => m.PerformedByUser)
            .OrderByDescending(m => m.CreatedAt);
        return (await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken),
            await query.CountAsync(cancellationToken));
    }

    public async Task AddAsync(InventoryMovement entity, CancellationToken cancellationToken = default)
    {
        await _db.InventoryMovements.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> GetReturnedQuantityAsync(Guid saleId, Guid productId, CancellationToken cancellationToken = default) =>
        _db.InventoryMovements
            .Where(m => m.SaleId == saleId && m.ProductId == productId && m.Type == "CustomerReturn")
            .SumAsync(m => m.QuantityChange, cancellationToken);

    public Task UpdateAsync(InventoryMovement entity, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Los movimientos de inventario son inmutables.");

    public Task DeleteAsync(InventoryMovement entity, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Los movimientos de inventario no se eliminan.");
}
