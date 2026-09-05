using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Interfaces;

public interface IUnitOfWork
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
