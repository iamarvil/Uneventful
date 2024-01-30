using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Uneventful.EventStore.Snapshot;

namespace Uneventful.EventStore;

public class EventStoreBuilder {
    public IServiceCollection Services { get; }
    public JsonSerializerOptions JsonSerializerOptions { get; }
    private SnapshotStoreBuilder? _snapshotStoreBuilder;

    public EventStoreBuilder(IServiceCollection services, JsonSerializerOptions jsonSerializerOptions) {
        Services = services;
        JsonSerializerOptions = jsonSerializerOptions;
    }
    
    public int DefaultSnapshotThreshold => _snapshotStoreBuilder?.DefaultSnapshotThreshold ?? 50;
    
    // todo: maybe i can let the user provide their own json serializer options here
    public void WithSnapshotStore(Action<SnapshotStoreBuilder> configure) {
        _snapshotStoreBuilder = new SnapshotStoreBuilder(Services, JsonSerializerOptions);
        configure(_snapshotStoreBuilder);
    }
}