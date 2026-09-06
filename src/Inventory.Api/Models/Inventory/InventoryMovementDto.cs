using System;

namespace Inventory.Api.Models.Inventory;

public record InventoryMovementDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid? SaleId,
    Guid? PurchaseId,
    Guid PerformedByUserId,
    string PerformedByUsername,
    int QuantityChange,
    int StockAfter,
    string Type,
    string Reason,
    DateTime CreatedAt);
