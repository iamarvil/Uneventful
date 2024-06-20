namespace Uneventful.EventStore.Mongo;

public static class EventStoreExtensions {
    public static MongoEventStore GetMongoEventStore(this IEventStore eventStore) => (MongoEventStore)eventStore;
}