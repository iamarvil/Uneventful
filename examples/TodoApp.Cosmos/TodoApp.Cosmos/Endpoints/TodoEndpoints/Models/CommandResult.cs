using System.Text.Json.Serialization;

namespace TodoApp.Cosmos.Endpoints.TodoEndpoints.Models;

public class CommandResult {
    public CommandResult(string id, long version, Dictionary<string, object>? metaData = null) {
        Id = id;
        Version = version;
        MetaData = metaData;
    }

    public string Id { get; init; }
    public long Version { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? MetaData { get; init; }
}