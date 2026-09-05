using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Inventory.Application.Interfaces;
using Inventory.Application.Reporting;
using Inventory.Application.Services;
using Inventory.Api.Constants;
using Inventory.Api.Models.Sale;
using Inventory.Api.Models.Responses;
using Inventory.Domain.Constants;
using Inventory.Domain.Entities;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly IRepository<Sale> _saleRepo;
    private readonly ISaleService _saleService;
    private readonly ISalesReportRepository _reportRepo;

    public SalesController(IRepository<Sale> saleRepo, ISaleService saleService, ISalesReportRepository reportRepo)
    {
        _saleRepo = saleRepo;
        _saleService = saleService;
        _reportRepo = reportRepo;
    }

    private static SaleDto ToDto(Sale s) => new(
        s.Id,
        s.CreatedByUserId,
        s.Lines.Select(line => new SaleLineDto(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice, line.Total)).ToList(),
        s.TotalItems,
        s.Total,
        s.Date);

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AppConstants.DefaultValues.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var (items, total) = await _saleRepo.ListPagedAsync(page, pageSize);
        var dtos = items.Select(ToDto).ToList();
        return Ok(PaginatedResponse<SaleDto>.Create(dtos, page, pageSize, total));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var s = await _saleRepo.GetByIdAsync(id);
        if (s is null) return NotFound(ApiResponse<SaleDto>.NotFound());

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole(Roles.Admin) && (!Guid.TryParse(userIdClaim, out var userId) || s.CreatedByUserId != userId))
            return Forbid();

        return Ok(ApiResponse<SaleDto>.Ok(ToDto(s)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSaleRequest req)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<SaleDto>.Unauthorized());

        var result = await _saleService.CreateSaleAsync(
            userId,
            req.Lines.Select(line => (line.ProductId, line.Quantity)).ToList());

        switch (result.Outcome)
        {
            case SaleOutcome.InvalidSale:
                return BadRequest(ApiResponse<SaleDto>.BadRequest(AppConstants.ErrorMessages.InvalidSale));
            case SaleOutcome.ProductNotFound:
                return NotFound(ApiResponse<SaleDto>.NotFound(AppConstants.ErrorMessages.ProductNotFound));
            case SaleOutcome.InsufficientStock:
                return BadRequest(ApiResponse<SaleDto>.BadRequest(AppConstants.ErrorMessages.InsufficientStock));
            case SaleOutcome.Conflict:
                return Conflict(ApiResponse<SaleDto>.Error(
                    "No se pudo registrar la venta porque el stock del producto cambió varias veces al mismo tiempo. Intenta de nuevo."));
        }

        var dto = ToDto(result.Sale!);
        return CreatedAtAction(nameof(Get), new { id = dto.Id },
            ApiResponse<SaleDto>.Ok(dto, AppConstants.SuccessMessages.SaleRegistered));
    }

    [HttpGet("reports/top-products")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> TopProducts([FromQuery] int top = 10)
    {
        if (top < 1) top = 10;
        if (top > 100) top = 100;

        var report = await _reportRepo.GetTopSellingProductsAsync(top);
        return Ok(ApiResponse<IEnumerable<ProductSalesSummary>>.Ok(report));
    }
}
