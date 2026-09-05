using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Interfaces;

namespace Inventory.Api.Tests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    private readonly ISnapshotable[] _repositories;

    public FakeUnitOfWork(params ISnapshotable[] repositories) => _repositories = repositories;

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var snapshots = _repositories.Select(r => r.Snapshot()).ToArray();
        try
        {
            return await operation(cancellationToken);
        }
        catch
        {
            for (var i = 0; i < _repositories.Length; i++)
                _repositories[i].Restore(snapshots[i]);
            throw;
        }
    }
}
