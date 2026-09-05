namespace Inventory.Api.Tests.Fakes;

public interface ISnapshotable
{
    object Snapshot();
    void Restore(object snapshot);
}
