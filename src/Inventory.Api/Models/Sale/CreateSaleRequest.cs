using System.ComponentModel.DataAnnotations;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Sale;

public record CreateSaleRequest(
    [Required(ErrorMessage = "At least one sale line is required")]
    [MinLength(1, ErrorMessage = "At least one sale line is required")]
    IReadOnlyCollection<CreateSaleLineRequest> Lines
);
