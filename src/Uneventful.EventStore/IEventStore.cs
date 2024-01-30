namespace Uneventful.EventStore;


public interface IEventStore {
    /// <summary>
    /// Commits event to the event store and returns the new stream version
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="event"></param>
    /// <param name="expectedVersion"></param>
    /// <param name="metaData"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>New version number</returns>
    public Task<long> AppendToStream(string streamId, EventBase @event, long expectedVersion, EventMetaData? metaData = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commits multiple events to the event store and returns the new stream version
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="events"></param>
    /// <param name="expectedVersion"></param>
    /// <param name="metaData"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>New version number</returns>
    public Task<long> AppendToStream(string streamId, EventBase[] events, long expectedVersion, EventMetaData? metaData = null, CancellationToken cancellationToken = default);
    
    public IAsyncEnumerable<EventWrapper<EventBase>> LoadStream(string streamId, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<EventWrapper<EventBase>> LoadStream(string streamId, long fromVersion, long? toVersion = null, CancellationToken cancellationToken = default);
}