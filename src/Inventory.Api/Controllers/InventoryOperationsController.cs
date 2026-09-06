using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Inventory.Api.Models.Inventory;
using Inventory.Api.Models.Responses;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Constants;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/inventory-operations")]
[Authorize(Roles = Roles.Admin)]
public class InventoryOperationsController : ControllerBase
{
    private readonly IInventoryMovementService _service;
    private readonly IInventoryMovementRepository _movementRepository;

    public InventoryOperationsController(
        IInventoryMovementService service,
        IInventoryMovementRepository movementRepository)
    {
        _service = service;
        _movementRepository = movementRepository;
    }

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AppConstants.DefaultValues.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var (items, total) = await _movementRepository.ListPagedAsync(page, pageSize);
        var dtos = items.Select(ToDto).ToList();
        return Ok(PaginatedResponse<InventoryMovementDto>.Create(dtos, page, pageSize, total));
    }

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
            InventoryMovementOutcome.ProductNotFound => NotFound(ApiResponse<object>.NotFound(
                AppConstants.ErrorMessages.ProductNotFound)),
            InventoryMovementOutcome.SaleNotFound => NotFound(ApiResponse<object>.NotFound(
                "La venta indicada no existe.")),
            InventoryMovementOutcome.ProductNotInSale => BadRequest(ApiResponse<object>.BadRequest(
                "El producto indicado no pertenece a esa venta.")),
            InventoryMovementOutcome.ReturnQuantityExceeded => BadRequest(ApiResponse<object>.BadRequest(
                "La cantidad devuelta supera la cantidad vendida o ya devuelta.")),
            InventoryMovementOutcome.Conflict => Conflict(ApiResponse<object>.Error("El inventario cambió al mismo tiempo. Intenta de nuevo.")),
            _ => BadRequest(ApiResponse<object>.BadRequest(
                "La operación no es válida o dejaría el stock negativo."))
        };
    }

    private static InventoryMovementDto ToDto(InventoryMovement movement) =>
        new(
            movement.Id,
            movement.ProductId,
            movement.Product?.Name ?? "Producto no disponible",
            movement.SaleId,
            movement.PurchaseId,
            movement.PerformedByUserId,
            movement.PerformedByUser?.Username ?? "Usuario no disponible",
            movement.QuantityChange,
            movement.StockAfter,
            movement.Type,
            movement.Reason,
            movement.CreatedAt);
}
