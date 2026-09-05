using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Reporting;

namespace Inventory.Application.Interfaces;

public interface ISalesReportRepository
{
    Task<IEnumerable<ProductSalesSummary>> GetTopSellingProductsAsync(int top = 10, CancellationToken cancellationToken = default);
}
