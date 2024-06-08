using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Uneventful.EventStore.Serialization;

namespace Uneventful.EventStore;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddEventStore(this IServiceCollection services, string domain, Action<EventStoreBuilder> configure) {
        var builder = new EventStoreBuilder(domain);
        
        configure(builder);
        
        if (!builder.JsonSerializerOptions.Converters.Any(x => x is EventWrapperConverter)) {
            var converter = new EventWrapperConverter();
            foreach (var domainEvents in builder.RegisteredEventTypes) {
                converter.RegisterDomainEvents(domainEvents.Key, domainEvents.Value);
            }
            builder.JsonSerializerOptions.Converters.Add(converter);
        }
        var eventStore = builder.Build();
        
        services.AddSingleton<IEventStore>(s => eventStore);
        
        return services;
    }
    
    
}