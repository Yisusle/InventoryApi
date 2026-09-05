using System.ComponentModel.DataAnnotations;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Purchase;

public record CreatePurchaseRequest(
    [Required(ErrorMessage = "Product ID is required")]
    Guid ProductId,

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, AppConstants.ValidationLimits.MaxStock, ErrorMessage = "Quantity must be between 1 and 1,000,000")]
    int Quantity,

    [Required(ErrorMessage = "Total cost is required")]
    [Range(AppConstants.ValidationLimits.MinPrice, AppConstants.ValidationLimits.MaxPrice, ErrorMessage = "Total cost must be between 0.01 and 999999.99")]
    decimal TotalCost
);