using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Services;

namespace Inventory.Application.Interfaces;

public interface ISaleService
{
    Task<SaleResult> CreateSaleAsync(
        Guid userId,
        IReadOnlyCollection<(Guid ProductId, int Quantity)> lines,
        CancellationToken cancellationToken = default);
}
