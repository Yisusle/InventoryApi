using System;
using System.Threading.Tasks;
using Inventory.Api.Controllers;
using Inventory.Api.Models.Category;
using Inventory.Api.Models.Responses;
using Inventory.Api.Tests.Fakes;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Inventory.Api.Tests.Controllers;

public class CategoriesControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsPaginatedCategories()
    {
        var repo = new InMemoryRepository<Category>(c => c.Id);
        await repo.AddAsync(new Category { Id = Guid.NewGuid(), Name = "Electronics" });
        await repo.AddAsync(new Category { Id = Guid.NewGuid(), Name = "Books" });
        var controller = new CategoriesController(repo);

        var actionResult = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<ApiResponse<PaginatedResponse<CategoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data!.Total);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        var repo = new InMemoryRepository<Category>(c => c.Id);
        var controller = new CategoriesController(repo);

        var actionResult = await controller.Get(Guid.NewGuid());

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
        var response = Assert.IsType<ApiResponse<CategoryDto>>(notFound.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Create_AddsCategory_AndReturnsCreatedAtAction()
    {
        var repo = new InMemoryRepository<Category>(c => c.Id);
        var controller = new CategoriesController(repo);

        var actionResult = await controller.Create(new CreateCategoryRequest("Toys"));

        var created = Assert.IsType<CreatedAtActionResult>(actionResult);
        var response = Assert.IsType<ApiResponse<CategoryDto>>(created.Value);
        Assert.Equal("Toys", response.Data!.Name);
        Assert.Single(repo.Items);
    }
}
