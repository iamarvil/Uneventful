using Microsoft.Extensions.Logging;
using Uneventful.EventStore;
using Uneventful.Repository.Snapshot;

namespace Uneventful.Repository;

public class AggregateRepository {
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore? _snapshotStore;
    private readonly ILogger<AggregateRepository>? _logger;
    public int DefaultSnapshotThreshold { get; internal init; }
    
    public AggregateRepository(IEventStore eventStore, ISnapshotStore? snapshotStore, ILoggerFactory? loggerFactory = null) {
        _eventStore = eventStore;
        _snapshotStore = snapshotStore;
        _logger = loggerFactory?.CreateLogger<AggregateRepository>();
    }
    
    private async Task Save<T>(T aggregate, EventMetaData? metaData, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        if (aggregate.Changes.Length == 0) {
            _logger?.LogDebug("No changes to save for {StreamId}", aggregate.StreamId);
            return;
        }

        metaData ??= new EventMetaData {
            CorrelationId = Guid.NewGuid()
        };

        var newVersion = await _eventStore.AppendToStream(aggregate.StreamId, aggregate.Changes, aggregate.Version, metaData, cancellationToken);
        aggregate.Version = newVersion;
        aggregate.ClearChanges();

        _logger?.LogDebug("Saved {StreamId} with new version {version}", aggregate.StreamId, newVersion);
    }

    public async Task SaveAsync<T>(T aggregate, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        await SaveAsync(aggregate, null, cancellationToken);
    }
    
    public async Task SaveAsync<T>(T aggregate, EventMetaData? metaData, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        await Save(aggregate, metaData, cancellationToken);
        // todo: consider fire and forget
        await SaveSnapshot(aggregate, cancellationToken: cancellationToken);
    }

    public async Task SaveAndForceSnapshot<T>(T aggregate, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        await SaveAndForceSnapshot(aggregate, null, cancellationToken);
    }

    public async Task SaveAndForceSnapshot<T>(T aggregate, EventMetaData? metaData, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        await Save(aggregate, metaData, cancellationToken);
        // todo: consider fire and forget
        await SaveSnapshot(aggregate, true, cancellationToken);
    }
    
    public async Task<T?> LoadAsync<T>(string streamId, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        if (string.IsNullOrWhiteSpace(streamId)) throw new ArgumentException("StreamId cannot be empty");

        T aggregate;
        if (_snapshotStore != null && typeof(ISnapshotCapable).IsAssignableFrom(typeof(T))) {
            _logger?.LogDebug("Loading snapshot for {StreamId}", streamId);
            var snapshot = await _snapshotStore.LoadSnapshot<T>(streamId, cancellationToken);
            if (snapshot != null) {
                aggregate = snapshot;
                _logger?.LogDebug("Snapshots loaded for {StreamId}. Loading events after version {startVersion}", streamId, aggregate.Version);
                await aggregate.Load(_eventStore.LoadStream(streamId, aggregate.Version + 1, cancellationToken: cancellationToken));
                return aggregate;
            }
            
            _logger?.LogDebug("No snapshot found for {StreamId}", streamId);
        }

        aggregate = new T();
        
        _logger?.LogDebug("Loading events for {StreamId} from beginning", streamId);
        await aggregate.Load(_eventStore.LoadStream(streamId, cancellationToken));
        
        _logger?.LogDebug("Loaded {StreamId} with version {version}", streamId, aggregate.Version);
        return aggregate.Version == 0 ? null : aggregate;
    }
    
    private async Task SaveSnapshot<T>(T aggregate, bool force = false, CancellationToken cancellationToken = default) where T : EventSourced, new() {
        if (_snapshotStore == null) {
            _logger?.LogDebug("No snapshot store configured");
            return;
        }

        if (aggregate is ISnapshotCapable snapshotCapable) {
            var snapshotRulePassed = snapshotCapable.SnapshotWhen ?? aggregate.Version % DefaultSnapshotThreshold == 0;
            if(force || snapshotRulePassed) {
                _logger?.LogDebug("Saving snapshot for {StreamId}. Forced: {forced}, SnapshotWhen: {snapshotWhen}",
                    aggregate.StreamId, force, snapshotCapable.SnapshotWhen);
                try {
                    await _snapshotStore.SaveSnapshot(aggregate, cancellationToken);
                } catch (Exception ex) {
                    _logger?.LogWarning(ex, "Failed to save snapshot for {StreamId}", aggregate.StreamId);
                }
            }
        }
    }
}