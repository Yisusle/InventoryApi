using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public record PurchaseResult(PurchaseOutcome Outcome, Purchase? Purchase = null);
