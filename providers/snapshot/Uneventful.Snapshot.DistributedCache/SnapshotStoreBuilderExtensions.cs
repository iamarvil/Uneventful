using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Uneventful.Repository.Snapshot;

namespace Uneventful.Snapshot.DistributedCache;

public static class SnapshotStoreBuilderExtensions {
    public static SnapshotStoreBuilder UseDistributedCache(this SnapshotStoreBuilder builder, DistributedCacheEntryOptions defaultCacheEntryOptions) {
        builder.Services.AddSingleton<ISnapshotStore, DistributedCacheSnapshotStore>(s => new DistributedCacheSnapshotStore(
            s.GetRequiredService<IDistributedCache>(),
            defaultCacheEntryOptions, 
            builder.JsonSerializerOptions
        ));
        return builder;
    }
}