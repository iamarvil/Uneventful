using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Uneventful.EventStore.Snapshot;

namespace Uneventful.EventStore;

public class EventStoreBuilder {
    public IServiceCollection Services { get; }
    public JsonSerializerOptions JsonSerializerOptions { get; }

    public EventStoreBuilder(IServiceCollection services, JsonSerializerOptions jsonSerializerOptions) {
        Services = services;
        JsonSerializerOptions = jsonSerializerOptions;
    }
}