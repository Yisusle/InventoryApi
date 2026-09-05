using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory.Domain.Entities;

public class Sale
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<SaleLine> Lines { get; private set; } = new List<SaleLine>();
    public int TotalItems => Lines.Sum(line => line.Quantity);
    public decimal Total => Lines.Sum(line => line.Total);

    public static Sale Create(Guid userId, IEnumerable<SaleLine> lines)
    {
        var lineList = lines.ToList();
        if (lineList.Count == 0)
            throw new ArgumentException("La venta debe contener al menos un producto.", nameof(lines));

        return new Sale
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = userId,
            Lines = lineList
        };
    }
}
