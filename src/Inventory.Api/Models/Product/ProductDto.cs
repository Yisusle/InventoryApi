using System;

namespace Inventory.Api.Models.Product;

public record ProductDto(Guid Id, string Name, string? Sku, Guid? CategoryId, decimal Price, int Stock, int MinimumStock, DateTime CreatedAt);