using Uneventful.EventStore.Snapshot;

namespace Uneventful.EventStore.Repository;

public class AggregateRepositoryBuilder {
    private IEventStore? _eventStore;
    private ISnapshotStore? _snapshotStore;
    
    public AggregateRepositoryBuilder UseEventStore(IEventStore eventStore) {
        _eventStore = eventStore;
        return this;
    }
    
    public AggregateRepositoryBuilder UseSnapshotStore(ISnapshotStore snapshotStore) {
        _snapshotStore = snapshotStore;
        return this;
    }
    
    public bool HasEventStore => _eventStore != null;

    public bool HasSnapshotStore => _snapshotStore != null;

    public AggregateRepository Build() {
        if (_eventStore == null) {
            throw new InvalidOperationException("EventStore must be set.");
        }
        return new AggregateRepository(_eventStore, _snapshotStore);
    }
}