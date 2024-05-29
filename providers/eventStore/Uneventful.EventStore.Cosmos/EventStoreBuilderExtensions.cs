﻿using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Uneventful.EventStore.Cosmos.Serializer;

namespace Uneventful.EventStore.Cosmos;

public static class EventStoreBuilderExtensions {
    
    public static CosmosEventStore UseCosmos(this EventStoreBuilder builder, CosmosClientBuilder clientBuilder, string databaseName, string containerName) {
        var client = clientBuilder
            .WithCustomSerializer(new CosmosEventWrapperSerializer(builder.JsonSerializerOptions))
            .Build();
        
        return new CosmosEventStore(client, databaseName, containerName);
    }
    
    public static CosmosEventStore UseCosmos(this EventStoreBuilder builder, string connectionString, string databaseName, string containerName, Action<CosmosClientBuilder>? configure = null) {
        var cosmosClientBuilder = new CosmosClientBuilder(connectionString);
        configure?.Invoke(cosmosClientBuilder);
        return builder.UseCosmos(cosmosClientBuilder, databaseName, containerName);
    }
    
    public static CosmosEventStore UseCosmos(this EventStoreBuilder builder, string endpoint, string key, string databaseName, string containerName, Action<CosmosClientBuilder>? configure = null) {
        var connectionString = $"AccountEndpoint={endpoint};AccountKey={key};";
        
        return UseCosmos(builder, connectionString, databaseName, containerName, configure);
    }
}