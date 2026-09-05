using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Api.Tests.Fakes;

public class FakeProductRepository : IProductRepository, ISnapshotable
{
    private Dictionary<Guid, Product> _products = new();

    public int FailNextUpdatesWithConflict { get; set; }

    public int UpdateCallCount { get; private set; }

    public void Seed(Product product) => _products[product.Id] = Clone(product);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _products.TryGetValue(id, out var product);
        return Task.FromResult(product is null ? null : Clone(product));
    }

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var product = _products.Values.FirstOrDefault(p => p.Sku == sku);
        return Task.FromResult(product is null ? null : Clone(product));
    }

    public Task<IEnumerable<Product>> ListLowStockAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Product>>(_products.Values
            .Where(product => product.Stock <= product.MinimumStock)
            .Select(Clone)
            .ToList());

    public Task<(IEnumerable<Product> Items, int TotalCount)> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = _products.Values.Select(Clone).ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IEnumerable<Product>)items, all.Count));
    }

    public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _products[entity.Id] = Clone(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        UpdateCallCount++;

        if (FailNextUpdatesWithConflict > 0)
        {
            FailNextUpdatesWithConflict--;
            throw new ConcurrencyConflictException("Conflicto de concurrencia simulado para pruebas.");
        }

        _products[entity.Id] = Clone(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _products.Remove(entity.Id);
        return Task.CompletedTask;
    }

    public object Snapshot() => _products.ToDictionary(kv => kv.Key, kv => Clone(kv.Value));

    public void Restore(object snapshot) => _products = (Dictionary<Guid, Product>)snapshot;

    private static Product Clone(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Sku = p.Sku,
        CategoryId = p.CategoryId,
        Price = p.Price,
        Stock = p.Stock,
        MinimumStock = p.MinimumStock,
        CreatedAt = p.CreatedAt,
        RowVersion = p.RowVersion
    };
}
