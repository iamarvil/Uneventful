using Microsoft.Extensions.DependencyInjection;
using Uneventful.EventStore;

namespace Uneventful.Repository;

public static class ServiceCollectionExtensions {
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