using System.Text.Json.Serialization;
using Uneventful.EventStore;

namespace Uneventful.Repository;

public abstract class EventSourced {
    public abstract string StreamId { get; }
    private readonly List<EventBase> _changes = [];
    public long Version { get; set; }
    private readonly Dictionary<string, Action<EventBase>> _eventHandlers = new();
    protected void RegisterHandler<T>(Action<T> handler) where T : EventBase {
        var type = typeof(T);
        _eventHandlers.Add(type.Name, (a) => handler((T)a));
    }

    protected virtual void Apply(EventBase @event) {
        _eventHandlers[@event.GetType().Name].Invoke(@event);
        _changes.Add(@event);
    }

    public async Task Load(IAsyncEnumerable<EventWrapper<EventBase>> events) {
        await foreach (var @event in events) {
            if (!_eventHandlers.TryGetValue(@event.EventType, out var handler)) continue;
            try {
                if (@event.Payload is { } payload) handler.Invoke(payload);
                Version = @event.Version;
            } catch (Exception ex) {
                // todo: create a load exception of sorts
                Console.WriteLine($"Error when applying \"{@event.EventType}\" on \"{this.GetType().Name}\": {ex.Message}");
                throw;
            }
        }
    }

    [JsonIgnore]
    public EventBase[] Changes => _changes.ToArray();
    public void ClearChanges() => _changes.Clear();
}