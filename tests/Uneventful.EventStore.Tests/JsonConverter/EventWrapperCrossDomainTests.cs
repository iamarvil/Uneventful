using System.Text.Json;
using Uneventful.EventStore.Serialization;

namespace Uneventful.EventStore.Tests.JsonConverter;

public class EventWrapperCrossDomainTests {
    
    private static EventWrapperConverter GetConverter() {
        var converter = new EventWrapperConverter();
        converter.RegisterDomainEvents("domain1", [typeof(Domain1Events.TestEvent)]);
        converter.RegisterDomainEvents("domain2", [typeof(Domain2Events.TestEvent)]);
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
        var eventWrapper = new EventWrapper<EventBase>("streamId", "TestEvent", new Domain1Events.TestEvent("value1", 2, 3.0m, new Domain1Events.TestObject("value1", 2, 3.0m)), "domain1", 1234567890, 1);
        
        var serialized = JsonSerializer.Serialize(eventWrapper, _options);
        var deserialized = JsonSerializer.Deserialize<EventWrapper<EventBase>>(serialized, _options);
        
        Assert.NotNull(deserialized);
        Assert.Equal(eventWrapper.StreamId, deserialized.StreamId);
        Assert.Equal(eventWrapper.EventType, deserialized.EventType);
        Assert.Equal(eventWrapper.Version, deserialized.Version);
        Assert.Equal(eventWrapper.Timestamp, deserialized.Timestamp);
        Assert.Equal(eventWrapper.Domain, deserialized.Domain);
        Assert.Equal(eventWrapper.Payload, deserialized.Payload);
        
        var eventWrapper2 = new EventWrapper<EventBase>("streamId", "TestEvent", new Domain2Events.TestEvent("value1", 2, 3.0m, new Domain2Events.TestObject("value1", 2, 3.0m), true), "domain2", 1234567890, 1);
        
        var serialized2 = JsonSerializer.Serialize(eventWrapper2, _options);
        var deserialized2 = JsonSerializer.Deserialize<EventWrapper<EventBase>>(serialized2, _options);
        
        Assert.NotNull(deserialized2);
        Assert.Equal(eventWrapper2.StreamId, deserialized2.StreamId);
        Assert.Equal(eventWrapper2.EventType, deserialized2.EventType);
        Assert.Equal(eventWrapper2.Version, deserialized2.Version);
        Assert.Equal(eventWrapper2.Timestamp, deserialized2.Timestamp);
        Assert.Equal(eventWrapper2.Domain, deserialized2.Domain);
        Assert.Equal(eventWrapper2.Payload, deserialized2.Payload);
    }

    private  static class Domain1Events {
        public record TestEvent(
            string Value1,
            int Value2,
            decimal Value3,
            TestObject Value4
        ) : EventBase;

        public  record TestObject(string Value1, int Value2, decimal Value3);
    }

    private static class Domain2Events {
        public  record TestEvent(
            string Value1,
            int Value2,
            decimal Value3,
            TestObject Value4,
            bool Value5
        ) : EventBase;

        public record TestObject(string Value1, int Value2, decimal Value3);
    }
}