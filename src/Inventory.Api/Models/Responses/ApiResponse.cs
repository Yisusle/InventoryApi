using System;
using System.Collections.Generic;
using Inventory.Api.Constants;

namespace Inventory.Api.Models.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = AppConstants.SuccessMessages.OperationCompleted)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> Error(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    public static ApiResponse<T> Unauthorized(string message = AppConstants.ErrorMessages.Unauthorized)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { "Authentication required" }
        };
    }

    public static ApiResponse<T> NotFound(string message = AppConstants.ErrorMessages.NotFound)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { "Resource not found" }
        };
    }

    public static ApiResponse<T> Forbidden(string message = AppConstants.ErrorMessages.Forbidden)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { "Forbidden" }
        };
    }

    public static ApiResponse<T> BadRequest(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string> { "Invalid request" }
        };
    }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse Ok(string message = AppConstants.SuccessMessages.OperationCompleted)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    public static ApiResponse Error(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages => (Total + PageSize - 1) / PageSize;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static ApiResponse<PaginatedResponse<T>> Create(
        List<T> items,
        int page,
        int pageSize,
        int total)
    {
        return ApiResponse<PaginatedResponse<T>>.Ok(new PaginatedResponse<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        });
    }
}
