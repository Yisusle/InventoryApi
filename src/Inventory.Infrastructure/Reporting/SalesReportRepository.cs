using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Inventory.Application.Interfaces;
using Inventory.Application.Reporting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Inventory.Infrastructure.Reporting;

public class SalesReportRepository : ISalesReportRepository
{
    private readonly string _connectionString;

    public SalesReportRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured");
    }

    public async Task<IEnumerable<ProductSalesSummary>> GetTopSellingProductsAsync(int top = 10, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP (@Top)
                sl.ProductId,
                sl.ProductName,
                SUM(sl.Quantity) AS TotalQuantitySold,
                SUM(sl.UnitPrice * sl.Quantity) AS TotalRevenue
            FROM dbo.SaleLines sl
            GROUP BY sl.ProductId, sl.ProductName
            ORDER BY SUM(sl.UnitPrice * sl.Quantity) DESC;";

        await using var connection = new SqlConnection(_connectionString);
        var command = new CommandDefinition(sql, new { Top = top }, cancellationToken: cancellationToken);
        return await connection.QueryAsync<ProductSalesSummary>(command);
    }
}
