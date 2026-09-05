using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Api.Tests.Fakes;

public class FakeInventoryMovementRepository : IInventoryMovementRepository, ISnapshotable
{
    private Dictionary<Guid, InventoryMovement> _items = new();
    public IReadOnlyCollection<InventoryMovement> Items => _items.Values.ToList();

    public Task<InventoryMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<(IEnumerable<InventoryMovement> Items, int TotalCount)> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var values = _items.Values.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IEnumerable<InventoryMovement>)values, _items.Count));
    }

    public Task AddAsync(InventoryMovement entity, CancellationToken cancellationToken = default)
    {
        _items[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<int> GetReturnedQuantityAsync(Guid saleId, Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Values.Where(m => m.SaleId == saleId && m.ProductId == productId && m.Type == "CustomerReturn").Sum(m => m.QuantityChange));

    public Task UpdateAsync(InventoryMovement entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DeleteAsync(InventoryMovement entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public object Snapshot() => new Dictionary<Guid, InventoryMovement>(_items);
    public void Restore(object snapshot) => _items = (Dictionary<Guid, InventoryMovement>)snapshot;
}
