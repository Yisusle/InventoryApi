using System;

namespace Inventory.Domain.Entities;

public class InventoryMovement
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public Guid? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public Guid PerformedByUserId { get; set; }
    public User? PerformedByUser { get; set; }
    public int QuantityChange { get; set; }
    public int StockAfter { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
