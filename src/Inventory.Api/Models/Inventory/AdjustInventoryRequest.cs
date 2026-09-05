using System.ComponentModel.DataAnnotations;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Inventory;

public record AdjustInventoryRequest(
    [Required] Guid ProductId,
    [Range(-AppConstants.ValidationLimits.MaxStock, AppConstants.ValidationLimits.MaxStock)]
    int QuantityChange,
    [Required, StringLength(500, MinimumLength = 3)]
    string Reason);
