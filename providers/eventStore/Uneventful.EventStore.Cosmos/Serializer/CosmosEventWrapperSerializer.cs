using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Uneventful.EventStore.Cosmos.Serializer;

public class CosmosEventWrapperSerializer : CosmosSerializer {
    private readonly JsonSerializerOptions _serializerOptions;

    public CosmosEventWrapperSerializer(JsonSerializerOptions serializerOptions) {
        _serializerOptions = serializerOptions;
    }
    
    public override T FromStream<T>(Stream stream) {
        using (stream) {
            if (typeof(Stream).IsAssignableFrom(typeof(T))) {
                return (T)(object)stream;
            }

            return System.Text.Json.JsonSerializer.Deserialize<T>(stream, _serializerOptions) ?? throw new NullReferenceException();
        }
    }

    public override Stream ToStream<T>(T input) {
        var outputStream = new MemoryStream();
        System.Text.Json.JsonSerializer.Serialize<T>(outputStream, input, _serializerOptions);
        outputStream.Position = 0;
        return outputStream;
    }
}