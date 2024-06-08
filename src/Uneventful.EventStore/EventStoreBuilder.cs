using System.Text.Json;

namespace Uneventful.EventStore;

public class EventStoreBuilder {
    public JsonSerializerOptions JsonSerializerOptions { get; private set; } = new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    public Dictionary<string, HashSet<Type>> RegisteredEventTypes { get; } = [];
    private Func<IEventStore>? EventStore { get; set; }
    
    public string? Domain { get; init; }

    public EventStoreBuilder(string domain) {
        Domain = domain;
    }
    
    public EventStoreBuilder RegisterEvent<TEvent>(string domain) where TEvent : EventBase {
        if (RegisteredEventTypes.TryGetValue(domain, out var eventTypes)) {
            eventTypes.Add(typeof(TEvent));
        }
        else {
            RegisteredEventTypes.Add(domain, [typeof(TEvent)]);
        }

        return this;
    }
    
    public EventStoreBuilder RegisterEventTypes(string domain, Type[] eventTypes) {
        foreach (var eventType in eventTypes) {
            if (!eventType.IsAssignableTo(typeof(EventBase))) continue;
            
            if (RegisteredEventTypes.TryGetValue(domain, out var types)) {
                types.Add(eventType);
            }
            else {
                RegisteredEventTypes.Add(domain, [eventType]);
            }
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