using System;

namespace Inventory.Api.Models.User;

public record UserDto(Guid Id, string Username, string Email, string Role, DateTime CreatedAt);
