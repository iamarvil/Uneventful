using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Uneventful.EventStore.Serialization;

namespace Uneventful.EventStore;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddEventStore(this IServiceCollection services, Action<EventStoreBuilder> configure) {
        var builder = new EventStoreBuilder();
        configure(builder);
        
        var converter = new EventWrapperConverter(builder.RegisteredEventTypes);

        if (builder.JsonSerializerOptions == null) {
            builder.ConfigureEventStoreJsonSerializerOptions(o => {
                o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.PropertyNameCaseInsensitive = true;
                o.Converters.Add(converter);
            });
        } else {
            builder.JsonSerializerOptions.Converters.Add(converter);
        }
        
        services.AddSingleton<IEventStore>(s => builder.EventStore ?? throw new InvalidOperationException("EventStore must be set."));
        
        return services;
    }
    
    
}