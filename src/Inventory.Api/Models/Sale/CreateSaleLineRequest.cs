using System.ComponentModel.DataAnnotations;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Sale;

public record CreateSaleLineRequest(
    [Required(ErrorMessage = "Product ID is required")]
    Guid ProductId,

    [Range(1, AppConstants.ValidationLimits.MaxStock, ErrorMessage = "Quantity must be between 1 and 1,000,000")]
    int Quantity);
