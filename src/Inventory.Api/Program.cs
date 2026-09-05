using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Data.Common;

using Microsoft.Data.SqlClient;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Reporting;
using Inventory.Infrastructure.Seed;
using Inventory.Domain.Entities;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Inventory.Infrastructure.Services;
using System.Text;

using Inventory.Api.Constants;

using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

using Serilog;
using Serilog.Events;

using Inventory.Api.Middleware;
using Inventory.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Inventory.Api")
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors.Select(e => $"{kv.Key}: {e.ErrorMessage}"))
                .ToList();

            return new BadRequestObjectResult(
                ApiResponse<object>.BadRequest(AppConstants.ErrorMessages.ValidationError, errors));
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inventory API",
        Version = "v1.0.0",
        Description = "API REST para gestión de inventario"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: Authorization: Bearer {token})",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(AppConstants.CorsPolicies.Default, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"];
if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    var key = Encoding.UTF8.GetBytes(jwtSecret);
    var jwtIssuer = jwtSection["Issuer"] ?? "inventory-api";
    var jwtAudience = jwtSection["Audience"] ?? "inventory-api-users";
    var jwtExpirationMinutes = jwtSection.GetValue<int?>("ExpirationMinutes")
        ?? AppConstants.DefaultValues.JwtExpirationMinutes;

    builder.Services.AddSingleton<IJwtTokenService>(
        _ => new JwtTokenService(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes));

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

    builder.Services.AddAuthorization();
}

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connStr))
{
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connStr));

    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IRepository<Category>, CategoryRepository>();
    builder.Services.AddScoped<IRepository<Purchase>, PurchaseRepository>();
    builder.Services.AddScoped<IRepository<Sale>, SaleRepository>();
    builder.Services.AddScoped<IRepository<InventoryMovement>, InventoryMovementRepository>();
    builder.Services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();

    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<ISaleService, SaleService>();
    builder.Services.AddScoped<IPurchaseService, PurchaseService>();
    builder.Services.AddScoped<IInventoryMovementService, InventoryMovementService>();

    builder.Services.AddScoped<ISalesReportRepository, SalesReportRepository>();
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(AppConstants.CorsPolicies.Default);

if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new { status = "Live" }));

app.MapGet("/health/ready", async (IConfiguration config, IServiceProvider sp) =>
{
    var conn = config.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(conn))
    {
        return Results.Ok(new { status = "Ready", details = "No DB configured: readiness checks skipped" });
    }

    try
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetService<AppDbContext>();
        if (db != null)
        {
            await db.Database.OpenConnectionAsync();
            await db.Database.CloseConnectionAsync();
        }

        return Results.Ok(new { status = "Ready", details = "DB reachable" });
    }
    catch (Exception)
    {
        return Results.StatusCode(503);
    }
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<AppDbContext>();
    if (db is not null)
    {
        try
        {
            await SeedData.EnsureSeedDataAsync(db);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Seed data failed to run on startup");
        }
    }
}

app.Run();
