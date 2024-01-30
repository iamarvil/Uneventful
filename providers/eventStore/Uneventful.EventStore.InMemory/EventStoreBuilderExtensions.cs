using Microsoft.Extensions.DependencyInjection;

namespace Uneventful.EventStore.InMemory;

public static class EventStoreBuilderExtensions {
    public static EventStoreBuilder UseInMemory(this EventStoreBuilder builder) {
        builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
        return builder;
    }
}