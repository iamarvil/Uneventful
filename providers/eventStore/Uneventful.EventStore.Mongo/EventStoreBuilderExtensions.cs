using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Uneventful.EventStore.Mongo.Serializer;

namespace Uneventful.EventStore.Mongo;

public static class EventStoreBuilderExtensions {
    
    public static EventStoreBuilder UseMongoEventStore(this EventStoreBuilder builder, MongoClient mongoClient, string databaseName, string collectionName, string domain) {
        if (builder.Domain == null) throw new InvalidOperationException("Domain must be set on the EventStoreBuilder");
        
        var serializer = new MongoEventWrapperSerializer();
        serializer.RegisterDomainEvents(domain, builder.RegisteredEventTypes[builder.Domain]);
        BsonSerializer.RegisterSerializationProvider(new EventWrapperSerializer(serializer));
        
        return builder.UseEventStore(() => new MongoEventStore(mongoClient, databaseName, collectionName, domain));
    }
    
    
    public static EventStoreBuilder UseMongoEventStore(this EventStoreBuilder builder, string connectionString, string databaseName, string collectionName, string domain) {
        var mongoClient = new MongoClient(connectionString);
        
        if (builder.Domain == null) throw new InvalidOperationException("Domain must be set on the EventStoreBuilder");
        
        return builder.UseMongoEventStore(mongoClient, databaseName, collectionName, domain);
    }

    public static EventStoreBuilder UseMongoEventStore(this EventStoreBuilder builder, string connectionString, string databaseName, string collectionName, string domain, Action<MongoClient>? configure = null) {
        
        var mongoClient = new MongoClient(connectionString);
        configure?.Invoke(mongoClient);
        
        return builder.UseMongoEventStore(mongoClient, databaseName, collectionName, domain);
    }
}