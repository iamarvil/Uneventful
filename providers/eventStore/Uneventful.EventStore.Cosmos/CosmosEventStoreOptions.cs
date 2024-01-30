using Microsoft.Azure.Cosmos;

namespace Uneventful.EventStore.Cosmos;

public class CosmosEventStoreOptions {
    public CosmosClientOptions? CosmosClientOptions { get; set; }
}