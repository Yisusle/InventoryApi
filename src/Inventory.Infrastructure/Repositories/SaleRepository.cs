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

public class SaleRepository : IRepository<Sale>
{
    private readonly AppDbContext _db;
    public SaleRepository(AppDbContext db) => _db = db;

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Sales
            .Include(s => s.Lines)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Sales.AsNoTracking().Include(s => s.Lines).OrderByDescending(s => s.Date);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Sale entity, CancellationToken cancellationToken = default)
    {
        await _db.Sales.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Sale entity, CancellationToken cancellationToken = default)
    {
        _db.Sales.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Sale entity, CancellationToken cancellationToken = default)
    {
        _db.Sales.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
