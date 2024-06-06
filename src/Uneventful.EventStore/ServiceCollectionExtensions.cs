using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Uneventful.EventStore.Serialization;

namespace Uneventful.EventStore;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddEventStore(this IServiceCollection services, Action<EventStoreBuilder> configure) {
        var builder = new EventStoreBuilder();
        
        configure(builder);
        
        var converter = new EventWrapperConverter(builder.RegisteredEventTypes);
        
        if (!builder.JsonSerializerOptions.Converters.Any(x => x is EventWrapperConverter)) {
            builder.JsonSerializerOptions.Converters.Add(converter);
        }
        var eventStore = builder.Build();
        
        services.AddSingleton<IEventStore>(s => eventStore);
        
        return services;
    }
    
    
}