using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public record SaleResult(SaleOutcome Outcome, Sale? Sale = null);
