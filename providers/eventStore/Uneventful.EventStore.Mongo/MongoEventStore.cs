using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Uneventful.EventStore.Exceptions;

namespace Uneventful.EventStore.Mongo;

public class MongoEventStore : IEventStore {
    private readonly MongoClient _mongoClient;
    private readonly string _domain;
    private readonly IMongoCollection<EventWrapper<EventBase>> _collection;
    private static readonly InsertOneOptions InsertOneOptions = new InsertOneOptions();
    private static readonly InsertManyOptions InsertManyOptions = new InsertManyOptions {
        IsOrdered = true
    };
    
    public IMongoCollection<EventWrapper<EventBase>> Collection => _collection;

    public MongoEventStore(MongoClient mongoClient, string databaseName, string collectionName, string domain) {
        _mongoClient = mongoClient;
        _domain = domain;

        var database = mongoClient.GetDatabase(databaseName);
        _collection = database.GetCollection<EventWrapper<EventBase>>(collectionName);
    }
    
    public async Task<long> AppendToStream(string streamId, EventBase @event, long expectedVersion, EventMetaData? metaData = null, CancellationToken cancellationToken = default) {
        var newVersion = expectedVersion + 1;
        var wrapper = new EventWrapper<EventBase>(
            streamId,
            @event.GetType().Name,
            @event,
            _domain,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            newVersion
        ) {
            MetaData = metaData
        };
        try {
            await _collection.InsertOneAsync(
                wrapper,
                InsertOneOptions,
                cancellationToken
            );

            return newVersion;
        } catch (MongoException ex) {
            if (ex is MongoWriteException writeException && writeException.WriteError.Category == ServerErrorCategory.DuplicateKey) {
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

        try {
            await _collection.InsertManyAsync(
                events.Select(@event => new EventWrapper<EventBase>(
                    streamId,
                    @event.GetType().Name,
                    @event,
                    _domain,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ++newVersion
                ) {
                    MetaData = metaData
                }),
                InsertManyOptions,
                cancellationToken: cancellationToken
            );

            return newVersion;
        } catch (MongoException ex) {
            if (ex is MongoWriteException writeException && writeException.WriteError.Category == ServerErrorCategory.DuplicateKey) {
                throw new EventStoreWriteConflictException(
                    $"Failed to append events to stream \"{streamId}\". Version {expectedVersion} is outdated.",
                    ex
                );
            }

            throw;
        }
    }

    public async IAsyncEnumerable<EventWrapper<EventBase>> LoadStream(string streamId, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        // using var events = await _collection
        //     .AsQueryable()
        //     .Where(x => x.StreamId.Equals(streamId))
        //     .OrderBy(x => x.Version)
        //     .ToCursorAsync(cancellationToken);

        var filter = Builders<EventWrapper<EventBase>>.Filter.Eq("streamId", streamId);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        
        while (await cursor.MoveNextAsync(cancellationToken)) {
            foreach (var @event in cursor.Current) {
                yield return @event;
            }
        }
    }

    public async IAsyncEnumerable<EventWrapper<EventBase>> LoadStream(string streamId, long fromVersion, long? toVersion = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        using var events = await _collection
            .AsQueryable()
            .Where(x => x.StreamId == streamId && x.Version >= fromVersion && (toVersion == null || x.Version <= toVersion))
            .OrderBy(x => x.Version)
            .ToCursorAsync(cancellationToken);
        
        while (await events.MoveNextAsync(cancellationToken)) {
            foreach (var @event in events.Current) {
                yield return @event;
            }
        }
    }
}