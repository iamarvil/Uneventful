using System.Text.Json.Serialization;

namespace Uneventful.EventStore;

public class EventWrapper<T> where T : EventBase, new() {

    private EventWrapper() {
        Id = string.Empty;
        StreamId = string.Empty;
        Version = 0;
        EventType = string.Empty;
        Timestamp = 0;
        Payload = new T();
    }

    public EventWrapper(string streamId, string eventType, T payload, long timeStamp, long version) {
        Id =  $"{streamId}:{version}";
        StreamId = streamId;
        Version = version;
        EventType = eventType;
        Timestamp = timeStamp;
        Payload = payload;
    }

    public string Id { get; init; }
    public string StreamId { get; init; }
    public string EventType { get; init; }
    public T Payload { get; init; }
    public long Timestamp { get; init; }
    public long Version { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EventMetaData? MetaData { get; init; }

    public override string ToString() {
        return $"{StreamId}:{EventType}:{Version}";
    }
}

public record EventMetaData {
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CausationId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CorrelationId { get; init; }
}