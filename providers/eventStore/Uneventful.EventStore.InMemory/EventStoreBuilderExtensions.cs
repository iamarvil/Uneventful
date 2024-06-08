namespace Uneventful.EventStore.InMemory;

public static class EventStoreBuilderExtensions {
    public static EventStoreBuilder UseInMemory(this EventStoreBuilder builder) {
        if (builder.Domain == null) throw new InvalidOperationException("Domain must be set on the EventStoreBuilder");
        builder.UseEventStore(() => new InMemoryEventStore(builder.Domain));
        return builder;
    }
}