using System.ComponentModel.DataAnnotations;

namespace Inventory.Api.Models.Auth;

public record LoginRequest(
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 3)]
    string Username,

    [Required(ErrorMessage = "Password is required")]
    [StringLength(500, MinimumLength = 6)]
    string Password
);
