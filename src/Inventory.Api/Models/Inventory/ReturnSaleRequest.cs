using System.ComponentModel.DataAnnotations;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Inventory;

public record ReturnSaleRequest(
    [Required] Guid SaleId,
    [Required] Guid ProductId,
    [Range(1, AppConstants.ValidationLimits.MaxStock)]
    int Quantity,
    [Required, StringLength(500, MinimumLength = 3)]
    string Reason);
