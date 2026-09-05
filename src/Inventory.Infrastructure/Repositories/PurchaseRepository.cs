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

public class PurchaseRepository : IRepository<Purchase>
{
    private readonly AppDbContext _db;
    public PurchaseRepository(AppDbContext db) => _db = db;

    public async Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Purchases.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<(IEnumerable<Purchase> Items, int TotalCount)> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Purchases.AsNoTracking().OrderByDescending(p => p.Date);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Purchase entity, CancellationToken cancellationToken = default)
    {
        await _db.Purchases.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Purchase entity, CancellationToken cancellationToken = default)
    {
        _db.Purchases.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Purchase entity, CancellationToken cancellationToken = default)
    {
        _db.Purchases.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
