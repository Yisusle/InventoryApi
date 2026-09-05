namespace Inventory.Application.Services;

public enum SaleOutcome
{
    Success,
    InvalidSale,
    ProductNotFound,
    InsufficientStock,
    Conflict
}
