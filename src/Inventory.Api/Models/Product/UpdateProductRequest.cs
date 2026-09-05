using System.ComponentModel.DataAnnotations;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Product;

public record UpdateProductRequest(
    [StringLength(AppConstants.ValidationLimits.ProductNameMaxLength, MinimumLength = AppConstants.ValidationLimits.ProductNameMinLength)]
    string? Name,

    [StringLength(AppConstants.ValidationLimits.ProductSkuMaxLength, MinimumLength = AppConstants.ValidationLimits.ProductSkuMinLength)]
    string? Sku,

    Guid? CategoryId,

    [Range(AppConstants.ValidationLimits.MinPrice, AppConstants.ValidationLimits.MaxPrice, ErrorMessage = "Price must be between 0.01 and 999999.99")]
    decimal? Price,

    [Range(0, AppConstants.ValidationLimits.MaxStock, ErrorMessage = "Minimum stock must be between 0 and 1,000,000")]
    int? MinimumStock
);