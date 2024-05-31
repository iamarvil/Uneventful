using System.Collections.Concurrent;
using Uneventful.Repository;
using Uneventful.Repository.Snapshot;

namespace Uneventful.Snapshot.InMemory;

public class InMemorySnapshotStore : ISnapshotStore {
    
    private readonly ConcurrentDictionary<string, EventSourced> _snapshots = new();
    
    public Task SaveSnapshot<T>(T aggregate, CancellationToken cancellationToken = default) where T : EventSourced {
        _snapshots[aggregate.StreamId] = aggregate;
        return Task.CompletedTask;
    }

    public Task<T?> LoadSnapshot<T>(string streamId, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        return _snapshots.TryGetValue(streamId, out var value) ? Task.FromResult((T?) value) : Task.FromResult<T?>(null);
    }
}