using System.ComponentModel.DataAnnotations;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Product;

public record CreateProductRequest(
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(AppConstants.ValidationLimits.ProductNameMaxLength, MinimumLength = AppConstants.ValidationLimits.ProductNameMinLength)]
    string Name,

    [StringLength(AppConstants.ValidationLimits.ProductSkuMaxLength, MinimumLength = AppConstants.ValidationLimits.ProductSkuMinLength)]
    string? Sku,

    Guid? CategoryId,

    [Required(ErrorMessage = "Price is required")]
    [Range(AppConstants.ValidationLimits.MinPrice, AppConstants.ValidationLimits.MaxPrice, ErrorMessage = "Price must be between 0.01 and 999999.99")]
    decimal Price,

    [Required(ErrorMessage = "Stock is required")]
    [Range(0, AppConstants.ValidationLimits.MaxStock, ErrorMessage = "Stock must be between 0 and 1,000,000")]
    int Stock,

    [Range(0, AppConstants.ValidationLimits.MaxStock, ErrorMessage = "Minimum stock must be between 0 and 1,000,000")]
    int MinimumStock = 0
);