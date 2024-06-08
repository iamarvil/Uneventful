using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uneventful.EventStore.Serialization;

public class EventWrapperConverter : JsonConverter<EventWrapper<EventBase>> {
    protected Dictionary<string, Dictionary<string, Type>> EventTypes { get; } = [];
    private string IdPropertyName { get; }
    protected string EventTypePropertyName { get; }
    protected string PayloadPropertyName { get; }
    protected string VersionPropertyName { get; }
    protected string StreamIdPropertyName { get; }
    protected string DomainPropertyName { get; }
    protected string TimestampPropertyName { get; }
    protected string MetadataPropertyName { get; }

    public EventWrapperConverter(
        string idPropertyName = "id",
        string eventTypePropertyName = "eventType",
        string payloadPropertyName = "payload",
        string versionPropertyName = "version",
        string streamIdPropertyName = "streamId",
        string domainPropertyName = "domain",
        string timestampPropertyName = "timestamp",
        string metadataPropertyName = "metadata"
    ) {
        IdPropertyName = idPropertyName;
        EventTypePropertyName = eventTypePropertyName;
        PayloadPropertyName = payloadPropertyName;
        VersionPropertyName = versionPropertyName;
        StreamIdPropertyName = streamIdPropertyName;
        DomainPropertyName = domainPropertyName;
        TimestampPropertyName = timestampPropertyName;
        MetadataPropertyName = metadataPropertyName;
    }
    
    public void RegisterDomainEvents(string domain, HashSet<Type> events) {
        if (EventTypes.TryGetValue(domain, out var eventType)) {
            foreach (var type in events) {
                eventType.Add(type.Name, type);
            }
        } else {
            EventTypes.Add(domain, events.ToDictionary(x => x.Name));
        }
    }
    
    public override EventWrapper<EventBase>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        string? streamId = null;
        long? version = null;
        string? eventType = null;
        string? payload = null;
        string? domain = null;
        long timestamp = 0;
        string? metadata = null;
        var startDepth = reader.CurrentDepth;
        EventWrapper<EventBase>? wrapper = null;
        
        
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.PropertyName) {
                var propertyName = reader.GetString();
                reader.Read();
                if (propertyName == EventTypePropertyName) {
                    eventType = reader.GetString();
                } else if (propertyName == PayloadPropertyName) {
                    var payloadElement = JsonElement.ParseValue(ref reader);
                    payload = payloadElement.GetRawText();
                } else if (propertyName == VersionPropertyName) {
                    version = reader.GetInt64();
                } else if (propertyName == StreamIdPropertyName) {
                    streamId = reader.GetString();
                } else if (propertyName == DomainPropertyName) {
                    domain = reader.GetString();
                } else if (propertyName == TimestampPropertyName) {
                    timestamp = reader.GetInt64();
                } else if (propertyName == MetadataPropertyName) {
                    var metadataElement = JsonElement.ParseValue(ref reader);
                    metadata = metadataElement.GetRawText();
                }
            }


            if (reader.TokenType != JsonTokenType.EndObject || reader.CurrentDepth != startDepth) continue;
            
            if (eventType != null && version != null && streamId != null && payload != null && domain != null) {
                if (EventTypes.TryGetValue(domain, out var eventTypes) && eventTypes.TryGetValue(eventType, out var type)) {
                    wrapper = new EventWrapper<EventBase>(
                        streamId,
                        eventType,
                        JsonSerializer.Deserialize(payload, type, options) as EventBase ?? new EventBase(),
                        domain,
                        timestamp,
                        version.Value
                    ) {
                        MetaData = metadata != null ? JsonSerializer.Deserialize<EventMetaData>(metadata, options) : null
                    };
                }
            }
                
            return wrapper ?? null;
        }
        
        return null;
    }

    public override void Write(Utf8JsonWriter writer, EventWrapper<EventBase> value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        
        writer.WriteString(IdPropertyName, value.Id);
        writer.WriteString(StreamIdPropertyName, value.StreamId);
        writer.WriteString(EventTypePropertyName, value.EventType);
        writer.WritePropertyName(PayloadPropertyName);
        JsonSerializer.Serialize(writer, value.Payload, value.Payload.GetType(), options);
        writer.WriteString(DomainPropertyName, value.Domain);
        writer.WriteNumber(TimestampPropertyName, value.Timestamp);
        writer.WriteNumber(VersionPropertyName, value.Version);
        if (value.MetaData != null) {
            writer.WritePropertyName(MetadataPropertyName);
            JsonSerializer.Serialize(writer, value.MetaData, value.MetaData.GetType(), options);
        }

        writer.WriteEndObject();
    }
}