using System.ComponentModel.DataAnnotations;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Category;

public record CreateCategoryRequest(
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(AppConstants.ValidationLimits.CategoryNameMaxLength, MinimumLength = AppConstants.ValidationLimits.CategoryNameMinLength)]
    string Name
);
