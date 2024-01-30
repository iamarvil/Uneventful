using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Uneventful.EventStore.Exceptions;

namespace Uneventful.EventStore.Cosmos;

public class CosmosEventStore : IEventStore, IDisposable {
    private readonly ItemRequestOptions _itemRequestOptions = new ItemRequestOptions {
        EnableContentResponseOnWrite = false,
    };
    private readonly CosmosClient _client;
    private readonly Container _container;
    
    internal CosmosEventStore(CosmosClient client, string databaseName, string containerName) {
        _client = client;
        _container = client.GetContainer(databaseName, containerName);
    }

    public async Task<long> AppendToStream(string streamId, EventBase @event, long expectedVersion, EventMetaData? metaData = null, CancellationToken cancellationToken = default) {
        var newVersion = expectedVersion + 1;
        try {
            var wrapper = new EventWrapper<EventBase>(
                streamId,
                @event.GetType().Name,
                @event,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                newVersion
            ) {
                MetaData = metaData
            };
            await _container.CreateItemAsync(wrapper, new PartitionKey(streamId), _itemRequestOptions, cancellationToken);
            return newVersion;
        } catch (CosmosException ex) {
            if (ex.StatusCode == HttpStatusCode.Conflict) {
                throw new EventStoreWriteConflictException(
                    $"Failed to append {@event.GetType().Name} event to stream \"{streamId}\". Version {expectedVersion} is outdated.", 
                    ex
                );
            }

            throw;
        }
    }

    public async Task<long> AppendToStream(string streamId, EventBase[] events, long expectedVersion, EventMetaData? metaData = null, CancellationToken cancellationToken = default) {
        if (events.Length == 1) {
            return await AppendToStream(streamId, events[0], expectedVersion, metaData, cancellationToken);
        }

        var newVersion = expectedVersion;
        var ctr = 0;
        const int batchMax = 100;

        var batchEvents = events.Take(batchMax).ToArray();

        while (batchEvents.Length != 0) {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId));

            foreach (var @event in batchEvents) {
                var wrapper = new EventWrapper<EventBase>(
                    streamId,
                    @event.GetType().Name,
                    @event,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ++newVersion
                ) {
                    MetaData = metaData
                };

                batch.CreateItem<EventWrapper<EventBase>>(wrapper);
            }

            var result = await batch.ExecuteAsync(cancellationToken);

            if (!result.IsSuccessStatusCode) {
                if (result.StatusCode == HttpStatusCode.Conflict) {

                    throw new EventStoreWriteConflictException(
                        $"Failed to append {events.Length} event(s) to stream \"{streamId}\". Version {expectedVersion} is outdated."
                    );
                }

                throw new Exception($"An error occurred while performing this action: {result.ErrorMessage}");
            }

            batchEvents = events.Skip(++ctr * batchMax).Take(batchMax).ToArray();
        }

        return newVersion;
    }

    public async IAsyncEnumerable<EventWrapper<EventBase>> LoadStream(string streamId, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        const string sqlQueryText = """
                                    SELECT e.id, e.streamId, e.version, e.eventType, e.payload, e.timestamp FROM events e
                                       WHERE e.streamId = @streamId
                                       ORDER BY e.version
                                    """;

        var queryDefinition = new QueryDefinition(sqlQueryText)
            .WithParameter("@streamId", streamId);

        await foreach (var wrapper in LoadAsync(streamId, queryDefinition, cancellationToken)) {
            yield return wrapper;
        }
    }

    public async IAsyncEnumerable<EventWrapper<EventBase>> LoadStream(string streamId, long fromVersion, long? toVersion = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        var sqlQueryText = "SELECT DOCUMENTID(e) position, e.streamId, e.version, e.eventType, e.payload, e.timestamp FROM events e"
                           + " WHERE e.streamId = @streamId"
                           + " AND e.version >= @fromVersion"
                           + (toVersion == null ? string.Empty : " AND e.version <= @toVersion")
                           + " ORDER BY e.version";

        var queryDefinition = new QueryDefinition(sqlQueryText)
            .WithParameter("@streamId", streamId)
            .WithParameter("@fromVersion", fromVersion)
            .WithParameter("@toVersion", toVersion);

        await foreach (var wrapper in LoadAsync(streamId, queryDefinition, cancellationToken)) {
            yield return wrapper;
        }
    }
    
    private async IAsyncEnumerable<EventWrapper<EventBase>> LoadAsync(string streamId, QueryDefinition queryDefinition, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        using var feedIterator = _container.GetItemQueryIterator<EventWrapper<EventBase>>(
            queryDefinition
        );

        while (feedIterator.HasMoreResults) {
            var response = await feedIterator.ReadNextAsync(cancellationToken);

            foreach (var eventWrapper in response) {
                yield return eventWrapper;
            }
        }
    }

    public void Dispose() {
        _client.Dispose();
    }
}