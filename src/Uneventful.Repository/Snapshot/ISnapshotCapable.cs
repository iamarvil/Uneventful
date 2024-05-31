using System.Text.Json.Serialization;

namespace Uneventful.Repository.Snapshot;

public interface ISnapshotCapable {
    [JsonIgnore] 
    public bool? SnapshotWhen { get; }
}