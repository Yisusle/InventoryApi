using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Inventory.Api.Constants;
using Inventory.Api.Models.Responses;

namespace Inventory.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items.ContainsKey(CorrelationIdMiddleware.HeaderName)
                ? context.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                : context.TraceIdentifier;

            _logger.LogError(ex, "Unhandled exception (CorrelationId: {CorrelationId})", correlationId);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = ApiResponse.Error(
                $"{AppConstants.ErrorMessages.ServerError}. Correlation ID: {correlationId}");

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
