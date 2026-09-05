using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Inventory.Api.Constants;
using Inventory.Api.Models.Auth;
using Inventory.Api.Models.Responses;
using Inventory.Api.Validators;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Services;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(IUserRepository userRepo, IJwtTokenService jwtTokenService)
    {
        _userRepo = userRepo;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (!CustomValidators.IsValidUsername(req.Username))
            return BadRequest(ApiResponse<AuthResponse>.BadRequest(AppConstants.ErrorMessages.InvalidUsername));

        if (!CustomValidators.IsValidPassword(req.Password))
            return BadRequest(ApiResponse<AuthResponse>.BadRequest(AppConstants.ErrorMessages.PasswordTooWeak));

        var existingUsername = await _userRepo.GetByUsernameAsync(req.Username);
        if (existingUsername is not null)
            return Conflict(ApiResponse<AuthResponse>.Error(AppConstants.ErrorMessages.UsernameTaken));

        var existingEmail = await _userRepo.GetByEmailAsync(req.Email);
        if (existingEmail is not null)
            return Conflict(ApiResponse<AuthResponse>.Error(AppConstants.ErrorMessages.EmailTaken));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = req.Username,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        await _userRepo.AddAsync(user);
        var token = _jwtTokenService.GenerateToken(user);
        return Ok(ApiResponse<AuthResponse>.Ok(
            new AuthResponse(token, user.Username, user.Role),
            AppConstants.SuccessMessages.UserRegistered));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _userRepo.GetByUsernameAsync(req.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(ApiResponse<AuthResponse>.Error(AppConstants.ErrorMessages.InvalidCredentials));

        var token = _jwtTokenService.GenerateToken(user);
        return Ok(ApiResponse<AuthResponse>.Ok(
            new AuthResponse(token, user.Username, user.Role),
            AppConstants.SuccessMessages.LoginSuccessful));
    }
}
