using System.Text.Json;

namespace Uneventful.EventStore;

public class EventStoreBuilder {
    public JsonSerializerOptions JsonSerializerOptions { get; private set; } = new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    public HashSet<Type> RegisteredEventTypes { get; } = [];
    private Func<IEventStore>? EventStore { get; set; }
    
    public EventStoreBuilder RegisterEvent<TEvent>() where TEvent : EventBase {
        RegisteredEventTypes.Add(typeof(TEvent));

        return this;
    }
    
    public EventStoreBuilder RegisterEventTypes(Type[] eventTypes) {
        foreach (var eventType in eventTypes) {
            if (!eventType.IsAssignableTo(typeof(EventBase))) continue;
            
            RegisteredEventTypes.Add(eventType);
        }

        return this;
    }
    
    public EventStoreBuilder UseEventStore(Func<IEventStore> eventStore) {
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

        return EventStore.Invoke();
    }
}