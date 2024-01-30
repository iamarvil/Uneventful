using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Uneventful.EventStore;
using Uneventful.EventStore.Snapshot;

namespace Uneventful.Snapshot.DistributedCache;

public class DistributedCacheSnapshotStore : ISnapshotStore {
    private readonly IDistributedCache _distributedCache;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly DistributedCacheEntryOptions _defaultCacheEntryOptions;

    public DistributedCacheSnapshotStore(IDistributedCache distributedCache, DistributedCacheEntryOptions defaultCacheEntryOptions, JsonSerializerOptions jsonSerializerOptions) {
        _distributedCache = distributedCache;
        _jsonSerializerOptions = jsonSerializerOptions;
        _defaultCacheEntryOptions = defaultCacheEntryOptions;
    }
    public async Task SaveSnapshot<T>(T aggregate, CancellationToken cancellationToken = default) where T : EventSourced {
        var json = JsonSerializer.SerializeToUtf8Bytes(aggregate, _jsonSerializerOptions);
        await _distributedCache.SetAsync(aggregate.StreamId, json, _defaultCacheEntryOptions, cancellationToken);
    }

    public async Task<T?> LoadSnapshot<T>(string streamId, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        var json = await _distributedCache.GetAsync(streamId, cancellationToken);
        if (json == null) return null;
        return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);
    }
}