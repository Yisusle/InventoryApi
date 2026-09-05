using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Inventory.Api.Models.Inventory;
using Inventory.Api.Models.Responses;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/inventory-operations")]
[Authorize(Roles = Roles.Admin)]
public class InventoryOperationsController : ControllerBase
{
    private readonly IInventoryMovementService _service;

    public InventoryOperationsController(IInventoryMovementService service) => _service = service;

    [HttpPost("adjustments")]
    public Task<IActionResult> Adjust(AdjustInventoryRequest request) =>
        Execute(async userId => await _service.AdjustAsync(userId, request.ProductId, request.QuantityChange, request.Reason));

    [HttpPost("returns")]
    public Task<IActionResult> Return(ReturnSaleRequest request) =>
        Execute(async userId => await _service.ReturnSaleAsync(userId, request.SaleId, request.ProductId, request.Quantity, request.Reason));

    private async Task<IActionResult> Execute(Func<Guid, Task<InventoryMovementResult>> operation)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(ApiResponse<object>.Unauthorized());

        var result = await operation(userId);
        return result.Outcome switch
        {
            InventoryMovementOutcome.Success => Ok(ApiResponse.Ok("Movimiento de inventario registrado.")),
            InventoryMovementOutcome.ProductNotFound or InventoryMovementOutcome.SaleNotFound => NotFound(ApiResponse<object>.NotFound()),
            InventoryMovementOutcome.Conflict => Conflict(ApiResponse<object>.Error("El inventario cambió al mismo tiempo. Intenta de nuevo.")),
            _ => BadRequest(ApiResponse<object>.BadRequest("La operación no es válida o dejaría el stock negativo."))
        };
    }
}
