using Microsoft.Extensions.DependencyInjection;
using Uneventful.EventStore.Snapshot;

namespace Uneventful.Snapshot.InMemory;

public static class SnapshotStoreBuilderExtensions {
    public static void UseInMemory(this SnapshotStoreBuilder builder) {
        builder.Services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
    }
}