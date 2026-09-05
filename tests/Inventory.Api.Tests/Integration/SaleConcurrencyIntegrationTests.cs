using System;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Inventory.Api.Tests.Integration;

[Trait("Category", "Integration")]
public class SaleConcurrencyIntegrationTests : IClassFixture<MsSqlContainerFixture>
{
    private readonly MsSqlContainerFixture _fixture;

    public SaleConcurrencyIntegrationTests(MsSqlContainerFixture fixture) => _fixture = fixture;

    private AppDbContext NewDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static SaleService NewSaleService(AppDbContext db)
    {
        var productRepo = new ProductRepository(db);
        var saleRepo = new SaleRepository(db);
        var movementRepo = new InventoryMovementRepository(db);
        var unitOfWork = new UnitOfWork(db);
        return new SaleService(unitOfWork, productRepo, saleRepo, movementRepo);
    }

    private async Task<Guid> SeedProductAsync(int stock)
    {
        var productId = Guid.NewGuid();
        await using var db = NewDbContext();
        if (!await db.Users.AnyAsync(user => user.Id == TestUserId))
        {
            db.Users.Add(new User
            {
                Id = TestUserId,
                Username = $"integration-user-{TestUserId}",
                Email = $"{TestUserId}@example.test",
                PasswordHash = "not-used",
                Role = "User"
            });
        }
        db.Products.Add(new Product
        {
            Id = productId,
            Name = $"Integration test product {productId}",
            Price = 10m,
            Stock = stock
        });
        await db.SaveChangesAsync();
        return productId;
    }

    private static readonly Guid TestUserId = Guid.NewGuid();

    private async Task<(int Stock, int SaleCount)> ReadFinalStateAsync(Guid productId)
    {
        await using var db = NewDbContext();
        var stock = await db.Products.AsNoTracking().Where(p => p.Id == productId).Select(p => p.Stock).SingleAsync();
        var saleCount = await db.SaleLines.CountAsync(line => line.ProductId == productId);
        return (stock, saleCount);
    }

    [Fact]
    public async Task ConcurrentSales_AgainstRealSqlServer_NeverLoseOrDoubleApplyAStockUpdate()
    {
        const int initialStock = 5;
        var productId = await SeedProductAsync(initialStock);

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var db = NewDbContext();
            return await NewSaleService(db).CreateSaleAsync(TestUserId, [(productId, 1)]);
        }));

        var successCount = results.Count(r => r.Outcome == SaleOutcome.Success);
        var (finalStock, saleCount) = await ReadFinalStateAsync(productId);

        Assert.True(finalStock >= 0, "Stock must never go negative.");
        Assert.Equal(initialStock - successCount, finalStock);
        Assert.Equal(successCount, saleCount);
        Assert.True(successCount <= initialStock);
    }

    [Fact]
    public async Task ConcurrentSales_AgainstRealSqlServer_NeverOversellWhenDemandExceedsStock()
    {
        const int initialStock = 3;
        const int concurrentRequests = 8;
        var productId = await SeedProductAsync(initialStock);

        var results = await Task.WhenAll(Enumerable.Range(0, concurrentRequests).Select(async _ =>
        {
            await using var db = NewDbContext();
            return await NewSaleService(db).CreateSaleAsync(TestUserId, [(productId, 1)]);
        }));

        var successCount = results.Count(r => r.Outcome == SaleOutcome.Success);
        var (finalStock, saleCount) = await ReadFinalStateAsync(productId);

        Assert.True(finalStock >= 0, "Stock must never go negative even under heavy contention.");
        Assert.True(successCount <= initialStock, "Can never sell more than was in stock.");
        Assert.Equal(initialStock - successCount, finalStock);
        Assert.Equal(successCount, saleCount);

        Assert.All(results, r => Assert.True(
            r.Outcome is SaleOutcome.Success or SaleOutcome.InsufficientStock or SaleOutcome.Conflict));
    }
}
