using System;

namespace Inventory.Api.Models.Purchase;

public record PurchaseDto(Guid Id, Guid ProductId, int Quantity, decimal TotalCost, DateTime Date);