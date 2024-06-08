using System.Text.Json;
using Microsoft.Azure.Cosmos.Fluent;
using Uneventful.EventStore.Cosmos.Serializer;
using Uneventful.EventStore.Serialization;

namespace Uneventful.EventStore.Cosmos;

public static class EventStoreBuilderExtensions {
    
    public static EventStoreBuilder UseCosmos(this EventStoreBuilder builder, CosmosClientBuilder clientBuilder, string databaseName, string containerName) {
        var converter = new EventWrapperConverter(timestampPropertyName: "_ts");
        
        if (builder.Domain == null) throw new InvalidOperationException("Domain must be set on the EventStoreBuilder");
        
        converter.RegisterDomainEvents(builder.Domain, builder.RegisteredEventTypes[builder.Domain]);
        builder.JsonSerializerOptions.Converters.Add(converter);
        
        var serializer = new CosmosEventWrapperSerializer(builder.JsonSerializerOptions);
        
        return builder.UseEventStore(() => {
            var client = clientBuilder
                .WithCustomSerializer(serializer)
                .Build();
            return new CosmosEventStore(client, databaseName, containerName, builder.Domain);
        });
    }
    
    public static EventStoreBuilder UseCosmos(this EventStoreBuilder builder, string connectionString, string databaseName, string containerName, Action<CosmosClientBuilder>? configure = null) {
        var cosmosClientBuilder = new CosmosClientBuilder(connectionString);
        configure?.Invoke(cosmosClientBuilder);
        return builder.UseCosmos(cosmosClientBuilder, databaseName, containerName);
    }
    
    public static EventStoreBuilder UseCosmos(this EventStoreBuilder builder, string endpoint, string key, string databaseName, string containerName, Action<CosmosClientBuilder>? configure = null) {
        var connectionString = $"AccountEndpoint={endpoint};AccountKey={key};";
        
        return UseCosmos(builder, connectionString, databaseName, containerName, configure);
    }
}