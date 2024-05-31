using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uneventful.EventStore.Serialization;

public class EventWrapperConverter : JsonConverter<EventWrapper<EventBase>> {
    private readonly HashSet<Type> _eventTypes;

    public EventWrapperConverter(HashSet<Type> eventTypes) {
        _eventTypes = eventTypes;
    }
    
    public override EventWrapper<EventBase>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        Type? type = null;
        string? streamId = null;
        long? version = null;
        string? eventType = null;
        string? payload = null;
        long? timestamp = null;
        string? metadata = null;
        var startDepth = reader.CurrentDepth;
        EventWrapper<EventBase>? wrapper = null;
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.PropertyName) {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName) {
                    case "eventType": {
                        eventType = reader.GetString();
                        
                        type = _eventTypes.FirstOrDefault(x => x.Name == eventType);
                        break;
                    }
                    case "payload": {
                        using var jsonDoc = JsonDocument.ParseValue(ref reader);
                        payload = jsonDoc.RootElement.GetRawText();
                        break;
                    }

                    case "version": {
                        version = reader.GetInt64();
                        break;
                    }
                    
                    case "streamId": {
                        streamId = reader.GetString();
                        break;
                    }

                    case "timestamp": {
                        timestamp = reader.GetInt64();
                        break;
                    }

                    case "metadata": {
                        using var jsonDoc = JsonDocument.ParseValue(ref reader);
                        metadata = jsonDoc.RootElement.GetRawText();
                        break;
                    
                    }
                }
            }


            if (reader.TokenType != JsonTokenType.EndObject || reader.CurrentDepth != startDepth) continue;
            
            if (eventType != null && version != null && streamId != null && type != null && payload != null && timestamp != null) {
                wrapper = new EventWrapper<EventBase>(
                    streamId,
                    eventType,
                    System.Text.Json.JsonSerializer.Deserialize(payload, type, options) as EventBase ?? new EventBase(),
                    timestamp.Value,
                    version.Value
                ) {
                    MetaData = metadata != null ? System.Text.Json.JsonSerializer.Deserialize<EventMetaData>(metadata, options) : null
                };
            }
                
            return wrapper ?? null;
        }
        
        return null;
    }

    public override void Write(Utf8JsonWriter writer, EventWrapper<EventBase> value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        
        writer.WriteString("id", value.Id);
        writer.WriteString("streamId", value.StreamId);
        writer.WriteString("eventType", value.EventType);
        writer.WritePropertyName("payload");
        System.Text.Json.JsonSerializer.Serialize(writer, value.Payload, value.Payload.GetType(), options);
        writer.WriteNumber("timestamp", value.Timestamp);
        writer.WriteNumber("version", value.Version);
        if (value.MetaData != null) {
            writer.WritePropertyName("metadata");
            System.Text.Json.JsonSerializer.Serialize(writer, value.MetaData, value.MetaData.GetType(), options);
        }

        writer.WriteEndObject();
    }
}