using System.Text.Json.Serialization;

namespace Uneventful.EventStore.Snapshot;

public interface ISnapshotCapable {
    [JsonIgnore] 
    public bool? SnapshotWhen { get; }
}