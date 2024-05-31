using Microsoft.Extensions.DependencyInjection;
using Uneventful.Repository.Snapshot;

namespace Uneventful.Snapshot.InMemory;

public static class SnapshotStoreBuilderExtensions {
    public static void UseInMemory(this SnapshotStoreBuilder builder) {
        builder.Services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
    }
}