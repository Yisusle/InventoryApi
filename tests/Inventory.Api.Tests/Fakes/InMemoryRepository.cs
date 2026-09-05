using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Interfaces;

namespace Inventory.Api.Tests.Fakes;

public class InMemoryRepository<T> : IRepository<T>, ISnapshotable where T : class
{
    private readonly Func<T, Guid> _idSelector;
    private Dictionary<Guid, T> _items = new();

    public InMemoryRepository(Func<T, Guid> idSelector) => _idSelector = idSelector;

    public IReadOnlyCollection<T> Items => _items.Values.ToList();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<(IEnumerable<T> Items, int TotalCount)> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var all = _items.Values.ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IEnumerable<T>)items, all.Count));
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        _items[_idSelector(entity)] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _items[_idSelector(entity)] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _items.Remove(_idSelector(entity));
        return Task.CompletedTask;
    }

    public object Snapshot() => new Dictionary<Guid, T>(_items);

    public void Restore(object snapshot) => _items = (Dictionary<Guid, T>)snapshot;
}
