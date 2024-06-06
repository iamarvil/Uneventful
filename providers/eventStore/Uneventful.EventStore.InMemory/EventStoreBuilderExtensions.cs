namespace Uneventful.EventStore.InMemory;

public static class EventStoreBuilderExtensions {
    public static EventStoreBuilder UseInMemory(this EventStoreBuilder builder) {
        builder.UseEventStore(() => new InMemoryEventStore());
        return builder;
    }
}