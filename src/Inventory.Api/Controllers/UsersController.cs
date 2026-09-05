using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Api.Constants;
using Inventory.Api.Models.Responses;
using Inventory.Api.Models.User;
using Inventory.Application.Interfaces;
using Inventory.Domain.Constants;
using Inventory.Domain.Entities;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repo;

    public UsersController(IUserRepository repo) => _repo = repo;

    private static UserDto ToDto(User u) => new(u.Id, u.Username, u.Email, u.Role, u.CreatedAt);

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var id))
            return Unauthorized(ApiResponse<UserDto>.Unauthorized());

        var user = await _repo.GetByIdAsync(id);
        if (user is null) return NotFound(ApiResponse<UserDto>.NotFound(AppConstants.ErrorMessages.UserNotFound));
        return Ok(ApiResponse<UserDto>.Ok(ToDto(user)));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AppConstants.DefaultValues.DefaultPageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var (items, total) = await _repo.ListPagedAsync(page, pageSize);
        var dtos = items.Select(ToDto).ToList();
        return Ok(PaginatedResponse<UserDto>.Create(dtos, page, pageSize, total));
    }
}
