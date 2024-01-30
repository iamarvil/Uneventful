# Basic Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddEventStore(o => {
        o.WithSnapshotStore(snapshotStoreBuilder => {
            snapshotStoreBuilder.UseMemoryCache();
        });
    });
```