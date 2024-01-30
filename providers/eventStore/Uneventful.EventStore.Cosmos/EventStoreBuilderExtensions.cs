using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Uneventful.EventStore.Cosmos.Serializer;

namespace Uneventful.EventStore.Cosmos;

public static class EventStoreBuilderExtensions {
    
    public static EventStoreBuilder UseCosmos(this EventStoreBuilder builder, CosmosClient client, string databaseName, string containerName) {
        client.ClientOptions.Serializer = new CosmosEventWrapperSerializer(builder.JsonSerializerOptions);
        
        if (builder.Services.All(x => x.ServiceType != typeof(CosmosClient))) {
            builder.Services.AddSingleton<CosmosClient>(client);
        }

        builder.Services.AddSingleton<IEventStore, CosmosEventStore>((s) => new CosmosEventStore(
            s.GetRequiredService<CosmosClient>(),
            databaseName,
            containerName
        ));

        return builder;
    }
    
    public static EventStoreBuilder UseCosmos(this EventStoreBuilder builder, string connectionString, string databaseName, string containerName, Action<CosmosEventStoreOptions>? configure = null) {
        var options = new CosmosEventStoreOptions();
        configure?.Invoke(options);
        var client = new CosmosClient(connectionString, options.CosmosClientOptions);
        builder.UseCosmos(client, databaseName, containerName);
        return builder;
    }
    
    public static EventStoreBuilder UseCosmos(this EventStoreBuilder builder, string endpoint, string key, string databaseName, string containerName, Action<CosmosEventStoreOptions>? configure = null) {
        var connectionString = $"AccountEndpoint={endpoint};AccountKey={key};";
        UseCosmos(builder, connectionString, databaseName, containerName, configure);
        
        return builder;
    }
}