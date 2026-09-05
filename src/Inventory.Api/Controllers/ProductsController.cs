using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inventory.Application.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Api.Constants;
using Inventory.Api.Models.Product;
using Inventory.Api.Models.Responses;
using Inventory.Api.Validators;
using Inventory.Domain.Constants;
using Inventory.Domain.Entities;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repo;

    public ProductsController(IProductRepository repo) => _repo = repo;

    private static ProductDto ToDto(Product p) =>
        new(p.Id, p.Name, p.Sku, p.CategoryId, p.Price, p.Stock, p.MinimumStock, p.CreatedAt);

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AppConstants.DefaultValues.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var (items, total) = await _repo.ListPagedAsync(page, pageSize);
        var dtos = items.Select(ToDto).ToList();
        return Ok(PaginatedResponse<ProductDto>.Create(dtos, page, pageSize, total));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var p = await _repo.GetByIdAsync(id);
        if (p is null) return NotFound(ApiResponse<ProductDto>.NotFound(AppConstants.ErrorMessages.ProductNotFound));
        return Ok(ApiResponse<ProductDto>.Ok(ToDto(p)));
    }

    [HttpGet("by-sku/{sku}")]
    public async Task<IActionResult> GetBySku(string sku)
    {
        var product = await _repo.GetBySkuAsync(sku);
        if (product is null) return NotFound(ApiResponse<ProductDto>.NotFound(AppConstants.ErrorMessages.ProductNotFound));
        return Ok(ApiResponse<ProductDto>.Ok(ToDto(product)));
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> LowStock()
    {
        var products = await _repo.ListLowStockAsync();
        return Ok(ApiResponse<IEnumerable<ProductDto>>.Ok(products.Select(ToDto)));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(CreateProductRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.Sku))
        {
            if (!CustomValidators.IsValidSku(req.Sku))
                return BadRequest(ApiResponse<ProductDto>.BadRequest(AppConstants.ErrorMessages.InvalidSku));

            var existing = await _repo.GetBySkuAsync(req.Sku);
            if (existing is not null)
                return Conflict(ApiResponse<ProductDto>.Error(AppConstants.ErrorMessages.SkuTaken));
        }

        var p = new Product
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Sku = req.Sku,
            CategoryId = req.CategoryId,
            Price = req.Price,
            Stock = req.Stock,
            MinimumStock = req.MinimumStock
        };

        await _repo.AddAsync(p);
        return CreatedAtAction(nameof(Get), new { id = p.Id },
            ApiResponse<ProductDto>.Ok(ToDto(p), AppConstants.SuccessMessages.ProductCreated));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest req)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return NotFound(ApiResponse<ProductDto>.NotFound(AppConstants.ErrorMessages.ProductNotFound));

        if (!string.IsNullOrEmpty(req.Sku) && req.Sku != existing.Sku)
        {
            if (!CustomValidators.IsValidSku(req.Sku))
                return BadRequest(ApiResponse<ProductDto>.BadRequest(AppConstants.ErrorMessages.InvalidSku));

            var withSameSku = await _repo.GetBySkuAsync(req.Sku);
            if (withSameSku is not null && withSameSku.Id != id)
                return Conflict(ApiResponse<ProductDto>.Error(AppConstants.ErrorMessages.SkuTaken));
        }

        if (!string.IsNullOrEmpty(req.Name))
            existing.Name = req.Name;

        if (!string.IsNullOrEmpty(req.Sku))
            existing.Sku = req.Sku;

        if (req.CategoryId.HasValue)
            existing.CategoryId = req.CategoryId.Value;

        if (req.Price.HasValue)
            existing.Price = req.Price.Value;

        if (req.MinimumStock.HasValue)
            existing.MinimumStock = req.MinimumStock.Value;

        try
        {
            await _repo.UpdateAsync(existing);
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(ApiResponse<ProductDto>.Error(ex.Message));
        }

        return Ok(ApiResponse<ProductDto>.Ok(ToDto(existing), AppConstants.SuccessMessages.ProductUpdated));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return NotFound(ApiResponse<object>.NotFound(AppConstants.ErrorMessages.ProductNotFound));

        try
        {
            await _repo.DeleteAsync(existing);
        }
        catch (EntityInUseException ex)
        {
            return Conflict(ApiResponse<object>.Error(ex.Message));
        }

        return Ok(ApiResponse.Ok(AppConstants.SuccessMessages.ProductDeleted));
    }
}
