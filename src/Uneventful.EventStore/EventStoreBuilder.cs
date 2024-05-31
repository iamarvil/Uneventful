using System.Text.Json;

namespace Uneventful.EventStore;

public class EventStoreBuilder {
    public JsonSerializerOptions? JsonSerializerOptions { get; private set; }
    public HashSet<Type> RegisteredEventTypes { get; } = [];
    private IEventStore? EventStore { get; set; }
    
    public EventStoreBuilder ConfigureEventStoreJsonSerializerOptions(Action<JsonSerializerOptions> configure) {
        JsonSerializerOptions = new JsonSerializerOptions();
        configure(JsonSerializerOptions);
        return this;
    }
    
    public EventStoreBuilder RegisterEvent<TEvent>() where TEvent : EventBase {
        RegisteredEventTypes.Add(typeof(TEvent));

        return this;
    }
    
    public EventStoreBuilder UseEventStore(IEventStore eventStore) {
        if (EventStore != null) {
            throw new InvalidOperationException("EventStore is already set.");
        }
        EventStore = eventStore;
        return this;
    }
    
    public IEventStore Build() {
        if (EventStore == null) {
            throw new InvalidOperationException("EventStore must be set.");
        }

        return EventStore;
    }
}