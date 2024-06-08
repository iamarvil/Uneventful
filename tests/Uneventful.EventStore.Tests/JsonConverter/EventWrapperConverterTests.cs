using System.Text.Json;
using Uneventful.EventStore.Serialization;

namespace Uneventful.EventStore.Tests.JsonConverter;

public class EventWrapperConverterTests {

    private static EventWrapperConverter GetConverter() {
        var converter = new EventWrapperConverter();
        converter.RegisterDomainEvents("domain", [typeof(TestEvent)]);
        return converter;
    }
    
    private readonly JsonSerializerOptions _options = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = {
            GetConverter()
        }
    };

    [Fact]
    public void Works() {
        var eventWrapper = new EventWrapper<EventBase>("streamId", nameof(TestEvent), new TestEvent("value1", 2, 3.0m, new TestObject("value1", 2, 3.0m)), "domain", 1234567890, 1);
        
        var serialized = JsonSerializer.Serialize(eventWrapper, _options);
        var deserialized = JsonSerializer.Deserialize<EventWrapper<EventBase>>(serialized, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(eventWrapper.StreamId, deserialized.StreamId);
        Assert.Equal(eventWrapper.EventType, deserialized.EventType);
        Assert.Equal(eventWrapper.Version, deserialized.Version);
        Assert.Equal(eventWrapper.Timestamp, deserialized.Timestamp);
        Assert.Equal(eventWrapper.Domain, deserialized.Domain);
        Assert.Equal(eventWrapper.Payload, deserialized.Payload);
    }

    private record TestEvent(
        string Value1,
        int Value2,
        decimal Value3,
        TestObject Value4
    ) : EventBase;

    private record TestObject(string Value1, int Value2, decimal Value3);
}