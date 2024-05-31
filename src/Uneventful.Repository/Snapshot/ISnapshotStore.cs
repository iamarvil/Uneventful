namespace Uneventful.Repository.Snapshot;

public interface ISnapshotStore {
    public Task SaveSnapshot<T>(T aggregate, CancellationToken cancellationToken = default) where T : EventSourced;
    public Task<T?> LoadSnapshot<T>(string streamId, CancellationToken cancellationToken = default) where T : EventSourced, new();
}