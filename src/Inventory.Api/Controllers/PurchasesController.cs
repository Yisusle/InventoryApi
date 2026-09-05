using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Api.Constants;
using Inventory.Api.Models.Purchase;
using Inventory.Api.Models.Responses;
using Inventory.Domain.Constants;
using Inventory.Domain.Entities;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class PurchasesController : ControllerBase
{
    private readonly IRepository<Purchase> _purchaseRepo;
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IRepository<Purchase> purchaseRepo, IPurchaseService purchaseService)
    {
        _purchaseRepo = purchaseRepo;
        _purchaseService = purchaseService;
    }

    private static PurchaseDto ToDto(Purchase p) => new(p.Id, p.ProductId, p.Quantity, p.TotalCost, p.Date);

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AppConstants.DefaultValues.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var (items, total) = await _purchaseRepo.ListPagedAsync(page, pageSize);
        var dtos = items.Select(ToDto).ToList();
        return Ok(PaginatedResponse<PurchaseDto>.Create(dtos, page, pageSize, total));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var p = await _purchaseRepo.GetByIdAsync(id);
        if (p is null) return NotFound(ApiResponse<PurchaseDto>.NotFound());
        return Ok(ApiResponse<PurchaseDto>.Ok(ToDto(p)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseRequest req)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<PurchaseDto>.Unauthorized());

        var result = await _purchaseService.CreatePurchaseAsync(userId, req.ProductId, req.Quantity, req.TotalCost);

        switch (result.Outcome)
        {
            case PurchaseOutcome.ProductNotFound:
                return NotFound(ApiResponse<PurchaseDto>.NotFound(AppConstants.ErrorMessages.ProductNotFound));
            case PurchaseOutcome.Conflict:
                return Conflict(ApiResponse<PurchaseDto>.Error(
                    "No se pudo registrar la compra porque el stock del producto cambió varias veces al mismo tiempo. Intenta de nuevo."));
        }

        var dto = ToDto(result.Purchase!);
        return CreatedAtAction(nameof(Get), new { id = dto.Id },
            ApiResponse<PurchaseDto>.Ok(dto, AppConstants.SuccessMessages.PurchaseRegistered));
    }
}
