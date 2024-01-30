using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TodoApp.Cosmos.Options;

namespace TodoApp.Cosmos.Utilities;

public class SetupCosmosDb {
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosOptions _options;

    public SetupCosmosDb(CosmosClient cosmosClient, IOptions<CosmosOptions> options) {
        _cosmosClient = cosmosClient;
        _options = options.Value;
    }
    
    public async Task RunAsync() {
        await CreateContainer(_cosmosClient, _options.EventStore.DatabaseName, _options.EventStore.ContainerName);
        await CreateLeaseContainer(_cosmosClient, _options.Leases.DatabaseName, _options.Leases.ContainerName);
    }

    private static async Task CreateContainer( CosmosClient client,string databaseName, string containerName) {
        var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
        var containerProperties = new ContainerProperties(containerName, "/streamId") {
            IndexingPolicy = new IndexingPolicy {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent,
                IncludedPaths = {
                    new IncludedPath { Path = "/streamId/?" }
                },
                ExcludedPaths = {
                    new ExcludedPath { Path = "/*" }
                },
                CompositeIndexes = {
                    new Collection<CompositePath> {
                        new() { Path = "/streamId" },
                        new() { Path = "/version", Order = CompositePathSortOrder.Ascending }
                    }
                }
            }
        };
        await database.Database.CreateContainerIfNotExistsAsync(containerProperties, throughput: 400);
    }

    private static async Task CreateLeaseContainer(CosmosClient client, string databaseName, string containerName) {
        var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
        var containerProperties = new ContainerProperties(containerName, "/id");
        await database.Database.CreateContainerIfNotExistsAsync(containerProperties, throughput: 400);
    }

}