using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public enum InventoryMovementOutcome
{
    Success,
    ProductNotFound,
    SaleNotFound,
    InvalidOperation,
    Conflict
}

public record InventoryMovementResult(InventoryMovementOutcome Outcome, InventoryMovement? Movement = null);
