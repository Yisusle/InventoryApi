using System;

namespace Inventory.Application.Reporting;

public record ProductSalesSummary(
    Guid ProductId,
    string ProductName,
    int TotalQuantitySold,
    decimal TotalRevenue);
