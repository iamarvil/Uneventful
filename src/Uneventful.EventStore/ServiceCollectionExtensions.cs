using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Uneventful.EventStore.Repository;
using Uneventful.EventStore.Utilities;

namespace Uneventful.EventStore;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddEventStore(this IServiceCollection services, Func<EventStoreBuilder, IEventStore> configure) {
        services.AddSingleton<IEventStore>(s => {
            var jsonSerializerOptions = s.GetService<JsonSerializerOptions>();
            if (jsonSerializerOptions == null || jsonSerializerOptions.Converters.All(x => x.GetType() != typeof(EventWrapperConverter))) {
                jsonSerializerOptions = new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    Converters = { new EventWrapperConverter(), }
                };
            }

            var builder = new EventStoreBuilder(services, jsonSerializerOptions);
            return configure(builder);
        });
        return services;
    }
    
    public static IServiceCollection AddAggregateRepository(this IServiceCollection services, Action<IServiceProvider, AggregateRepositoryBuilder>? configure = null) {
        services.AddSingleton<AggregateRepository>(s => {
            var builder = new AggregateRepositoryBuilder();
            configure?.Invoke(s, builder);
            
            if (!builder.HasEventStore) {
                var eventStore = s.GetService<IEventStore>();
                if (eventStore == null) {
                    throw new InvalidOperationException("EventStore must be set.");
                }

                builder.UseEventStore(eventStore);
            }
            
            return builder.Build();
        });
        return services;
    }
}