using Microsoft.Extensions.Caching.Memory;
using Uneventful.EventStore;
using Uneventful.EventStore.Snapshot;

namespace Uneventful.Snapshot.MemoryCache;

public class MemoryCacheSnapshotStore : ISnapshotStore {
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions? _defaultCacheOptions;

    public MemoryCacheSnapshotStore(IMemoryCache memoryCache, MemoryCacheEntryOptions? defaultCacheOptions = null) {
        _memoryCache = memoryCache;
        _defaultCacheOptions = defaultCacheOptions;
    }

    public Task SaveSnapshot<T>(T aggregate, CancellationToken cancellationToken = default) where T : EventSourced {
        _memoryCache.Set(aggregate.StreamId, aggregate, _defaultCacheOptions);
        return Task.CompletedTask;
    }

    public Task<T?> LoadSnapshot<T>(string streamId, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        _memoryCache.TryGetValue(streamId, out T? value);
        return Task.FromResult(value);
    }
}