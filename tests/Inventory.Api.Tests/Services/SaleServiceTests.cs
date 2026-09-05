using System;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Api.Tests.Fakes;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Api.Tests.Services;

public class SaleServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static Product NewProduct(int stock) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test product",
        Price = 10m,
        Stock = stock,
        RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
    };

    [Fact]
    public async Task CreateSaleAsync_ReturnsProductNotFound_WhenProductDoesNotExist()
    {
        var productRepo = new FakeProductRepository();
        var saleRepo = new InMemoryRepository<Sale>(s => s.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new SaleService(new FakeUnitOfWork(productRepo, saleRepo, movementRepo), productRepo, saleRepo, movementRepo);

        var result = await service.CreateSaleAsync(UserId, [(Guid.NewGuid(), 1)]);

        Assert.Equal(SaleOutcome.ProductNotFound, result.Outcome);
    }

    [Fact]
    public async Task CreateSaleAsync_ReturnsInsufficientStock_AndLeavesStockUntouched()
    {
        var productRepo = new FakeProductRepository();
        var product = NewProduct(stock: 3);
        productRepo.Seed(product);
        var saleRepo = new InMemoryRepository<Sale>(s => s.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new SaleService(new FakeUnitOfWork(productRepo, saleRepo, movementRepo), productRepo, saleRepo, movementRepo);

        var result = await service.CreateSaleAsync(UserId, [(product.Id, 5)]);

        Assert.Equal(SaleOutcome.InsufficientStock, result.Outcome);
        var stored = await productRepo.GetByIdAsync(product.Id);
        Assert.Equal(3, stored!.Stock);
        Assert.Empty(saleRepo.Items);
    }

    [Fact]
    public async Task CreateSaleAsync_DecrementsStockAndRecordsSale_OnSuccess()
    {
        var productRepo = new FakeProductRepository();
        var product = NewProduct(stock: 10);
        productRepo.Seed(product);
        var saleRepo = new InMemoryRepository<Sale>(s => s.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new SaleService(new FakeUnitOfWork(productRepo, saleRepo, movementRepo), productRepo, saleRepo, movementRepo);

        var result = await service.CreateSaleAsync(UserId, [(product.Id, 4)]);

        Assert.Equal(SaleOutcome.Success, result.Outcome);
        Assert.Equal(4, result.Sale!.TotalItems);
        Assert.Equal(10m, result.Sale.Lines.Single().UnitPrice);
        Assert.Equal(40m, result.Sale.Total);

        var stored = await productRepo.GetByIdAsync(product.Id);
        Assert.Equal(6, stored!.Stock);
        Assert.Single(saleRepo.Items);
        Assert.Single(movementRepo.Items);
    }

    [Fact]
    public async Task CreateSaleAsync_RetriesAndSucceeds_WhenConcurrencyConflictIsTransient()
    {
        var productRepo = new FakeProductRepository { FailNextUpdatesWithConflict = 2 };
        var product = NewProduct(stock: 10);
        productRepo.Seed(product);
        var saleRepo = new InMemoryRepository<Sale>(s => s.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new SaleService(new FakeUnitOfWork(productRepo, saleRepo, movementRepo), productRepo, saleRepo, movementRepo);

        var result = await service.CreateSaleAsync(UserId, [(product.Id, 4)]);

        Assert.Equal(SaleOutcome.Success, result.Outcome);
        var stored = await productRepo.GetByIdAsync(product.Id);
        Assert.Equal(6, stored!.Stock);
        Assert.Single(saleRepo.Items);
    }

    [Fact]
    public async Task CreateSaleAsync_ReturnsConflict_WhenConcurrencyConflictNeverResolves()
    {
        var productRepo = new FakeProductRepository { FailNextUpdatesWithConflict = 100 };
        var product = NewProduct(stock: 10);
        productRepo.Seed(product);
        var saleRepo = new InMemoryRepository<Sale>(s => s.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new SaleService(new FakeUnitOfWork(productRepo, saleRepo, movementRepo), productRepo, saleRepo, movementRepo);

        var result = await service.CreateSaleAsync(UserId, [(product.Id, 4)]);

        Assert.Equal(SaleOutcome.Conflict, result.Outcome);
        var stored = await productRepo.GetByIdAsync(product.Id);
        Assert.Equal(10, stored!.Stock);
        Assert.Empty(saleRepo.Items);
    }

    [Fact]
    public async Task CreateSaleAsync_CalculatesTotalFromTheCurrentProductPrice()
    {
        var productRepo = new FakeProductRepository();
        var product = NewProduct(stock: 10);
        product.Price = 12.50m;
        productRepo.Seed(product);
        var saleRepo = new InMemoryRepository<Sale>(s => s.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new SaleService(new FakeUnitOfWork(productRepo, saleRepo, movementRepo), productRepo, saleRepo, movementRepo);

        var result = await service.CreateSaleAsync(UserId, [(product.Id, 3)]);

        Assert.Equal(SaleOutcome.Success, result.Outcome);
        Assert.Equal(12.50m, result.Sale!.Lines.Single().UnitPrice);
        Assert.Equal(37.50m, result.Sale.Total);
    }

    [Fact]
    public async Task CreateSaleAsync_CombinesRepeatedProductsIntoOneAuditableLine()
    {
        var productRepo = new FakeProductRepository();
        var product = NewProduct(stock: 10);
        productRepo.Seed(product);
        var saleRepo = new InMemoryRepository<Sale>(s => s.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new SaleService(new FakeUnitOfWork(productRepo, saleRepo, movementRepo), productRepo, saleRepo, movementRepo);

        var result = await service.CreateSaleAsync(UserId, [(product.Id, 2), (product.Id, 3)]);

        Assert.Equal(SaleOutcome.Success, result.Outcome);
        Assert.Single(result.Sale!.Lines);
        Assert.Equal(5, result.Sale.TotalItems);
        Assert.Equal(50m, result.Sale.Total);
        Assert.Equal(-5, movementRepo.Items.Single().QuantityChange);
    }
}
