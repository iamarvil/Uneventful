using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Uneventful.Repository.Snapshot;

public class SnapshotStoreBuilder {
    public IServiceCollection Services { get; }
    public JsonSerializerOptions JsonSerializerOptions { get; }
    public int DefaultSnapshotThreshold { get; set; } = 50;
    
    public SnapshotStoreBuilder(IServiceCollection services, JsonSerializerOptions jsonSerializerOptions) {
        Services = services;
        JsonSerializerOptions = jsonSerializerOptions;
    }
}