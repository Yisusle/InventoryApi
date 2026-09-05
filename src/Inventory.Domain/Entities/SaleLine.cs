using System;

namespace Inventory.Domain.Entities;

public class SaleLine
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Total => UnitPrice * Quantity;

    public static SaleLine Create(Product product, int quantity)
    {
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        return new SaleLine
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            UnitPrice = product.Price
        };
    }
}
