using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public enum InventoryMovementOutcome
{
    Success,
    ProductNotFound,
    SaleNotFound,
    ProductNotInSale,
    ReturnQuantityExceeded,
    InvalidOperation,
    Conflict
}

public record InventoryMovementResult(InventoryMovementOutcome Outcome, InventoryMovement? Movement = null);
