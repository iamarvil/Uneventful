# Basic Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services
.AddEventStore(o => {
    o.UseCosmos(
      builder.Configuration.GetConnectionString("your-cosmos-db-connectionstring-key"),
      "cosmos-db-name",
      "cosmos-db-container"
    );
});
```