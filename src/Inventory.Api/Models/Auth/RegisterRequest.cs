using System.ComponentModel.DataAnnotations;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Auth;

public record RegisterRequest(
    [Required(ErrorMessage = "Username is required")]
    [StringLength(AppConstants.ValidationLimits.UsernameMaxLength, MinimumLength = AppConstants.ValidationLimits.UsernameMinLength)]
    string Username,

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(AppConstants.ValidationLimits.EmailMaxLength)]
    string Email,

    [Required(ErrorMessage = "Password is required")]
    [StringLength(500, MinimumLength = AppConstants.ValidationLimits.PasswordMinLength, ErrorMessage = "Password must be at least 8 characters")]
    string Password
);
