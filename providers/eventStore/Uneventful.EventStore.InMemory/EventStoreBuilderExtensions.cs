namespace Uneventful.EventStore.InMemory;

public static class EventStoreBuilderExtensions {
    public static EventStoreBuilder UseInMemory(this EventStoreBuilder builder) {
        builder.UseEventStore((_) => new InMemoryEventStore());
        return builder;
    }
}