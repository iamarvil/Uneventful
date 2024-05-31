using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Uneventful.Repository.Snapshot;

namespace Uneventful.Snapshot.MemoryCache;

public static class SnapshotStoreBuilderExtensions {
    public static void UseMemoryCache(this SnapshotStoreBuilder builder, MemoryCacheEntryOptions? defaultCacheOptions = null) {
        builder.Services.AddSingleton<ISnapshotStore, MemoryCacheSnapshotStore>(s => 
            new MemoryCacheSnapshotStore(
                s.GetRequiredService<IMemoryCache>(),
                defaultCacheOptions ?? new MemoryCacheEntryOptions()
            )
        );
        
    }
}