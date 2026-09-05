using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inventory.Application.Interfaces;
using Inventory.Domain.Constants;
using Inventory.Domain.Entities;
using Inventory.Api.Constants;
using Inventory.Api.Models.Category;
using Inventory.Api.Models.Responses;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IRepository<Category> _repo;
    public CategoriesController(IRepository<Category> repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AppConstants.DefaultValues.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var (items, total) = await _repo.ListPagedAsync(page, pageSize);
        var dtos = items.Select(c => new CategoryDto(c.Id, c.Name)).ToList();
        return Ok(PaginatedResponse<CategoryDto>.Create(dtos, page, pageSize, total));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var c = await _repo.GetByIdAsync(id);
        if (c is null) return NotFound(ApiResponse<CategoryDto>.NotFound(AppConstants.ErrorMessages.CategoryNotFound));
        return Ok(ApiResponse<CategoryDto>.Ok(new CategoryDto(c.Id, c.Name)));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(CreateCategoryRequest req)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = req.Name
        };
        await _repo.AddAsync(category);
        var dto = new CategoryDto(category.Id, category.Name);
        return CreatedAtAction(nameof(Get), new { id = category.Id },
            ApiResponse<CategoryDto>.Ok(dto, AppConstants.SuccessMessages.CategoryCreated));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryRequest req)
    {
        var c = await _repo.GetByIdAsync(id);
        if (c is null) return NotFound(ApiResponse<CategoryDto>.NotFound(AppConstants.ErrorMessages.CategoryNotFound));
        c.Name = req.Name ?? c.Name;
        await _repo.UpdateAsync(c);
        return Ok(ApiResponse<CategoryDto>.Ok(new CategoryDto(c.Id, c.Name), AppConstants.SuccessMessages.CategoryUpdated));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await _repo.GetByIdAsync(id);
        if (c is null) return NotFound(ApiResponse<object>.NotFound(AppConstants.ErrorMessages.CategoryNotFound));
        await _repo.DeleteAsync(c);
        return Ok(ApiResponse.Ok(AppConstants.SuccessMessages.CategoryDeleted));
    }
}
