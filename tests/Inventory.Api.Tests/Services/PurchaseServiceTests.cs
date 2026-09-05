using System;
using System.Threading.Tasks;
using Inventory.Api.Tests.Fakes;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Api.Tests.Services;

public class PurchaseServiceTests
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
    public async Task CreatePurchaseAsync_ReturnsProductNotFound_WhenProductDoesNotExist()
    {
        var productRepo = new FakeProductRepository();
        var purchaseRepo = new InMemoryRepository<Purchase>(p => p.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new PurchaseService(new FakeUnitOfWork(productRepo, purchaseRepo, movementRepo), productRepo, purchaseRepo, movementRepo);

        var result = await service.CreatePurchaseAsync(UserId, Guid.NewGuid(), 5, 50m);

        Assert.Equal(PurchaseOutcome.ProductNotFound, result.Outcome);
    }

    [Fact]
    public async Task CreatePurchaseAsync_IncrementsStockAndRecordsPurchase_OnSuccess()
    {
        var productRepo = new FakeProductRepository();
        var product = NewProduct(stock: 10);
        productRepo.Seed(product);
        var purchaseRepo = new InMemoryRepository<Purchase>(p => p.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new PurchaseService(new FakeUnitOfWork(productRepo, purchaseRepo, movementRepo), productRepo, purchaseRepo, movementRepo);

        var result = await service.CreatePurchaseAsync(UserId, product.Id, 15, 150m);

        Assert.Equal(PurchaseOutcome.Success, result.Outcome);
        Assert.Equal(15, result.Purchase!.Quantity);

        var stored = await productRepo.GetByIdAsync(product.Id);
        Assert.Equal(25, stored!.Stock);
        Assert.Single(purchaseRepo.Items);
        Assert.Single(movementRepo.Items);
    }

    [Fact]
    public async Task CreatePurchaseAsync_RetriesAndSucceeds_WhenConcurrencyConflictIsTransient()
    {
        var productRepo = new FakeProductRepository { FailNextUpdatesWithConflict = 2 };
        var product = NewProduct(stock: 10);
        productRepo.Seed(product);
        var purchaseRepo = new InMemoryRepository<Purchase>(p => p.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new PurchaseService(new FakeUnitOfWork(productRepo, purchaseRepo, movementRepo), productRepo, purchaseRepo, movementRepo);

        var result = await service.CreatePurchaseAsync(UserId, product.Id, 15, 150m);

        Assert.Equal(PurchaseOutcome.Success, result.Outcome);
        var stored = await productRepo.GetByIdAsync(product.Id);
        Assert.Equal(25, stored!.Stock);
        Assert.Single(purchaseRepo.Items);
    }

    [Fact]
    public async Task CreatePurchaseAsync_ReturnsConflict_WhenConcurrencyConflictNeverResolves()
    {
        var productRepo = new FakeProductRepository { FailNextUpdatesWithConflict = 100 };
        var product = NewProduct(stock: 10);
        productRepo.Seed(product);
        var purchaseRepo = new InMemoryRepository<Purchase>(p => p.Id);
        var movementRepo = new InMemoryRepository<InventoryMovement>(m => m.Id);
        var service = new PurchaseService(new FakeUnitOfWork(productRepo, purchaseRepo, movementRepo), productRepo, purchaseRepo, movementRepo);

        var result = await service.CreatePurchaseAsync(UserId, product.Id, 15, 150m);

        Assert.Equal(PurchaseOutcome.Conflict, result.Outcome);
        var stored = await productRepo.GetByIdAsync(product.Id);
        Assert.Equal(10, stored!.Stock);
        Assert.Empty(purchaseRepo.Items);
    }
}
