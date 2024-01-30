using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uneventful.EventStore.Repository;
using Uneventful.EventStore.Snapshot;
using Uneventful.EventStore.Utilities;

namespace Uneventful.EventStore;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddEventStore(this IServiceCollection services, Action<EventStoreBuilder> configure) {
        var jsonSerializerOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new EventWrapperConverter(), }
        };
        var builder = new EventStoreBuilder(services, jsonSerializerOptions);
        configure(builder);
        services.AddSingleton<AggregateRepository>((s) => {
            return new AggregateRepository(
                s.GetRequiredService<IEventStore>(),
                s.GetService<ISnapshotStore>(),
                s.GetService<ILoggerFactory>()
            ) {
                DefaultSnapshotThreshold = builder.DefaultSnapshotThreshold
            };
        });
        
        return services;
    }
}