using System;
using System.Threading.Tasks;
using Inventory.Api.Tests.Fakes;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Api.Tests.Services;

public class InventoryMovementServiceTests
{
    [Fact]
    public async Task AdjustAsync_RecordsReasonAndPreventsNegativeStock()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Test", Price = 10m, Stock = 3 };
        var products = new FakeProductRepository();
        products.Seed(product);
        var sales = new InMemoryRepository<Sale>(sale => sale.Id);
        var movements = new FakeInventoryMovementRepository();
        var service = new InventoryMovementService(new FakeUnitOfWork(products, sales, movements), products, sales, movements);

        var result = await service.AdjustAsync(Guid.NewGuid(), product.Id, -2, "Producto dañado");

        Assert.Equal(InventoryMovementOutcome.Success, result.Outcome);
        Assert.Equal("Producto dañado", result.Movement!.Reason);
        Assert.Equal(1, (await products.GetByIdAsync(product.Id))!.Stock);

        var invalid = await service.AdjustAsync(Guid.NewGuid(), product.Id, -2, "Conteo físico");
        Assert.Equal(InventoryMovementOutcome.InvalidOperation, invalid.Outcome);
    }
}
