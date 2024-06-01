namespace Uneventful.EventStore.Cosmos;

public static class EventStoreExtensions {
    public static CosmosEventStore GetCosmosEventStore(this IEventStore eventStore) => (CosmosEventStore)eventStore;

}