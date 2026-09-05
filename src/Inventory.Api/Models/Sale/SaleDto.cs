namespace Inventory.Api.Models.Sale;

public record SaleLineDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Total);

public record SaleDto(
    Guid Id,
    Guid CreatedByUserId,
    IReadOnlyCollection<SaleLineDto> Lines,
    int TotalItems,
    decimal Total,
    DateTime Date);
