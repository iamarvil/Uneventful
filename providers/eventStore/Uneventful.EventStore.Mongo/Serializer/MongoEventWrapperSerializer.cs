using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Uneventful.EventStore.Mongo.Serializer;

public class MongoEventWrapperSerializer : SerializerBase<EventWrapper<EventBase>>, IBsonDocumentSerializer {
    protected Dictionary<string, Dictionary<string, Type>> EventTypes { get; } = [];
    
    public void RegisterDomainEvents(string domain, HashSet<Type> events) {
        if (EventTypes.TryGetValue(domain, out var eventType)) {
            foreach (var type in events) {
                eventType.Add(type.Name, type);
            }
        } else {
            EventTypes.Add(domain, events.ToDictionary(x => x.Name));
        }
    }
    
    public override EventWrapper<EventBase> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
        
        context.Reader.ReadStartDocument();
        
        var id = context.Reader.ReadObjectId("_id");
        var streamId = context.Reader.ReadString("streamId");
        var eventType = context.Reader.ReadString("eventType");
        var payload = context.Reader.ReadString("payload");
        var domain = context.Reader.ReadString("domain");
        var timestamp = context.Reader.ReadInt64("timestamp");
        var version = context.Reader.ReadInt64("version");
        var metadata = context.Reader.ReadString("metaData");
        context.Reader.ReadEndDocument();
        
        if (!EventTypes.TryGetValue(domain, out var eventTypeMap)) {
            throw new InvalidOperationException($"No event types registered for domain \"{domain}\"");
        }
        
        if (!eventTypeMap.TryGetValue(eventType, out var type)) {
            throw new InvalidOperationException($"No event type registered for domain \"{domain}\" with name \"{eventType}\"");
        }


        var payloadObject = JsonSerializer.Deserialize(payload, type) as EventBase;
        if (payloadObject == null) {
            throw new InvalidOperationException($"Failed to deserialize payload for event type \"{eventType}\"");
        }
        
        var wrapper = new EventWrapper<EventBase>(
            streamId,
            eventType,
            payloadObject,
            domain,
            timestamp,
            version
        ) {
            MetaData = metadata != null ? BsonSerializer.Deserialize<EventMetaData>(metadata) : null
        };
        
        return wrapper;
    }

    public bool TryGetMemberSerializationInfo(string memberName, [UnscopedRef] out BsonSerializationInfo? serializationInfo) {
        switch (memberName) {
            case "StreamId":
            case "EventType":
            case "Payload":
            case "MetaData":
            case "Domain":
                serializationInfo = new BsonSerializationInfo(memberName, new StringSerializer(), typeof(string));
                break;
            case "Timestamp":
            case "Version":
                serializationInfo = new BsonSerializationInfo(memberName, new Int64Serializer(), typeof(long));
                break;
            default:
                serializationInfo = null;
                return false;
        }

        return true;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EventWrapper<EventBase> value) {
        context.Writer.WriteStartDocument();
        context.Writer.WriteString("streamId", value.StreamId);
        context.Writer.WriteString("eventType", value.EventType);
        context.Writer.WriteString("payload", JsonSerializer.Serialize(value.Payload, value.Payload.GetType()));
        context.Writer.WriteString("domain", value.Domain);
        context.Writer.WriteInt64("timestamp", value.Timestamp);
        context.Writer.WriteInt64("version", value.Version);
        context.Writer.WriteString("metaData", value.MetaData != null ? JsonSerializer.Serialize<EventMetaData>(value.MetaData) : null);
        context.Writer.WriteEndDocument();
    }
}

public class EventWrapperSerializer : IBsonSerializationProvider {
    private readonly MongoEventWrapperSerializer _serializer;

    public EventWrapperSerializer(MongoEventWrapperSerializer serializer) {
        _serializer = serializer;
    }
    
    public IBsonSerializer? GetSerializer(Type type) {
        return type == typeof(EventWrapper<EventBase>) ? _serializer : null;
    }
}