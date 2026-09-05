using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Domain.Constants;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Data;
using BCrypt.Net;

namespace Inventory.Infrastructure.Seed;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!db.Categories.Any())
        {
            var categories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Electronics", CreatedAt = DateTime.UtcNow },
                new Category { Id = Guid.NewGuid(), Name = "Clothing", CreatedAt = DateTime.UtcNow },
                new Category { Id = Guid.NewGuid(), Name = "Food", CreatedAt = DateTime.UtcNow },
                new Category { Id = Guid.NewGuid(), Name = "Books", CreatedAt = DateTime.UtcNow }
            };
            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();
        }

        if (!db.Products.Any())
        {
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Laptop", Price = 999.99m, Stock = 50, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.NewGuid(), Name = "Mouse", Price = 29.99m, Stock = 200, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.NewGuid(), Name = "Keyboard", Price = 79.99m, Stock = 150, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.NewGuid(), Name = "Monitor", Price = 299.99m, Stock = 75, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.NewGuid(), Name = "USB Cable", Price = 9.99m, Stock = 500, CreatedAt = DateTime.UtcNow }
            };
            db.Products.AddRange(products);
            await db.SaveChangesAsync();
        }

        if (!db.Users.Any(u => u.Role == Roles.Admin))
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!@#"),
                Role = Roles.Admin,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
        }

        if (!db.Users.Any(u => u.Username == "user"))
        {
            var regularUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "user",
                Email = "user@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!@#"),
                Role = Roles.User,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(regularUser);
            await db.SaveChangesAsync();
        }
    }
}
