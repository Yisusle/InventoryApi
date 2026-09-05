using System;

namespace Inventory.Domain.Entities;

public class Purchase
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public int Quantity { get; set; }
    public decimal TotalCost { get; set; }
}
