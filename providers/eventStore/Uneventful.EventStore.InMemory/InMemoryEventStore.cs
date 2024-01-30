using System.Runtime.CompilerServices;

namespace Uneventful.EventStore.InMemory;

public class InMemoryEventStore : IEventStore {
    public readonly Dictionary<string, Dictionary<string, EventWrapper<EventBase>>> EventStore = new ();
    public readonly List<EventWrapper<EventBase>> Events = [];
    
    public Task<long> AppendToStream(string streamId, EventBase @event, long expectedVersion, EventMetaData? metaData = null, CancellationToken cancellationToken = default) {
        var newVersion = expectedVersion + 1;
        if (!EventStore.ContainsKey(streamId)) EventStore[streamId] = new Dictionary<string, EventWrapper<EventBase>>();
        var wrapper = new EventWrapper<EventBase>(streamId, @event.GetType().Name, @event, DateTimeOffset.Now.ToUnixTimeSeconds(), newVersion) {
            MetaData = metaData
        };

        EventStore[streamId][wrapper.Id] = wrapper;
        Events.Add(wrapper);
        return Task.FromResult(newVersion);
    }

    public Task<long> AppendToStream(string streamId, EventBase[] events, long expectedVersion, EventMetaData? metaData = null, CancellationToken cancellationToken = default) {
        if (events.Length == 1) {
            return AppendToStream(streamId, events[0], expectedVersion,  metaData, cancellationToken);
        }

        var newVersion = expectedVersion;

        foreach (var @event in events) {
            if (!EventStore.ContainsKey(streamId)) EventStore[streamId] = new Dictionary<string, EventWrapper<EventBase>>();
            newVersion += 1;
            var wrapper = new EventWrapper<EventBase>(streamId, @event.GetType().Name, @event, DateTimeOffset.Now.ToUnixTimeSeconds(), newVersion);
            EventStore[streamId][wrapper.Id] = wrapper;
            Events.Add(wrapper);
        }

        return Task.FromResult(newVersion);
    }

    public async IAsyncEnumerable<EventWrapper<EventBase>> LoadStream(string streamId, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        if (!EventStore.TryGetValue(streamId, out var value)) {
            yield break;
        }

        foreach (var eventWrapper in value.Select(x => x.Value).OrderBy(x => x.Version)) {
            yield return eventWrapper;
        }
        
        await Task.CompletedTask;
    }

    public async IAsyncEnumerable<EventWrapper<EventBase>> LoadStream(string streamId, long fromVersion, long? toVersion = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        if (!EventStore.TryGetValue(streamId, out var eventStream)) {
            yield break;
        }

        var enumerable = eventStream
            .Select(x => x.Value)
            .OrderBy(x => x.Version)
            .Where(x => x.Version >= fromVersion && (toVersion == null || x.Version <= toVersion));

        foreach (var eventWrapper in enumerable) {
            yield return eventWrapper;
        }
        
        await Task.CompletedTask;
    }
}