using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        await _db.Products.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _db.Products.Remove(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new EntityInUseException(
                "No se puede eliminar el producto porque tiene compras o ventas registradas.", ex);
        }
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Products.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _db.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }

    public async Task<IEnumerable<Product>> ListLowStockAsync(CancellationToken cancellationToken = default) =>
        await _db.Products.AsNoTracking()
            .Where(p => p.Stock <= p.MinimumStock)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<(IEnumerable<Product> Items, int TotalCount)> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Products.AsNoTracking().OrderBy(p => p.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _db.Products.Update(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _db.Entry(entity).State = EntityState.Detached;
            throw new ConcurrencyConflictException(
                "El producto fue modificado por otra operación al mismo tiempo.", ex);
        }
    }
}
