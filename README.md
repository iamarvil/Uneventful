# Just a simple Event Sourcing library

**Uneventful** library is a simple and lightweight event sourcing library that provides an 
opinionated way of storing and retrieving events in an Event Store. 
This repository includes working CosmosDB and InMemory (for testing purposes) 
IEventStore implementations, a simple Aggregate Repository for building and mutating 
aggregates from a stream of events, an EventProcessor abstract class to simplify 
event processing, and a simple IMemoryCache implementation for ISnapshotStore.

## Basic Usage

> _For a working example, check out the `TodoApp.Cosmos` example project._

The following code demonstrates basic usage of the library.

### Registering the `AggregateRepository` to Dependency Injection

> *Note*: DI Setup will still change in the near future

```csharp
var builder = WebApplication.CreateBuilder(args);
const string domainName = "some-domain-name";
builder.Services
    .AddEventStore(
        domainName,
        eventStoreBuilder => {
           // required step: register a domain's events (for type caching on deserialization)
           eventStoreBuilder
               .RegisterEventTypes(domainName, [
                   typeof(SomeEvent), typeof(SomeOtherEvent)
               ]);
            
           // or, add the Uneventful.CodeGenerator package to your domain project and just do
           // eventStoreBuilder.AddSomeDomainEventTypes("some-domain-name");
           // or 
           // eventStoreBuilder.RegisterEventTypes("some-domain-name", Some.Domain.EventTypeRegistry.Events);
           
           // required step: register an event store provider
           eventStoreBuilder.UseInMemory();
            
          // or, if you want to use the cosmos provider
          // eventStoreBuilder.UseCosmos(
          //    builder.Configuration.GetConnectionString("your-cosmos-db-connection-string-key"),
          //    "cosmos-db-name",
          //    "cosmos-db-container"
          // );
            
          // optional step: register your snap shot store provider
          eventStoreBuilder.WithSnapshotStore(snapshotStoreBuilder => {
              snapshotStoreBuilder.UseInMemory();
              snapshotStoreBuilder.DefaultSnapshotThreshold = 50;
          });
            
          // optional step: make changes to the json serializer options
          // if you plan to use enums on events
          eventStoreBuilder.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }
    )
    // required step: add the Aggregate Repository to the DI
    .AddAggregateRepository();
```

> *Word of caution*: The library will respect any changes you make on the JsonSerializerOptions
> The defaults are pretty simple with just 
> ```
> PropertyNamingPolicy = JsonNamingPolicy.CamelCase
> PropertyNameCaseInsensitive = true
> ```

### Domain Model Sample Aggregate

```csharp
public class Todo : EventSourced {
    public static string GetStreamId(Guid id) => $"todo:{id}";
    public override string StreamId => GetStreamId(Id);
    public Guid Id { get; private set; }
    public string? Title { get; private set; }

    public Todo() {
        RegisterHandler<TodoItemCreated>(When);
    }

    public void Create(Guid id, string title) {
        if (Id != Guid.Empty) throw new Exception("Duplicate Todo ID");
        Apply(new TodoItemCreated(id, title));
    }

    private void When(TodoItemCreated @event) {
        Id = @event.Id;
        Title = @event.Title;
    }
}

```

### Snapshot Capable Domain Model Sample Aggregate

The `ISnapshotCapable` interface allows the `AggregateRepository` to identify aggregates that support snapshotting. When implemented, the `AggregateRepository.SaveAsync` method checks the `SnapshotWhen` property to determine whether to create a snapshot. This is useful for optimizing the rehydration of aggregates by reducing the number of events that need to be replayed.

Here's an example of an aggregate that creates a snapshot every 50 changes:

```csharp
public class Todo : EventSourced, ISnapshotCapable {
    public static string GetStreamId(Guid id) => $"todo:{id}";
    public override string StreamId => GetStreamId(Id);
    public bool? SnapshotWhen => Version % 50 == 0; // snapshot every 50 versions, or null for default
    public Guid Id { get; private set; }
    public string? Title { get; private set; }

    public Todo() {
        RegisterHandler<TodoItemCreated>(When);
    }

    public void Create(Guid id, string title) {
        if (Id != Guid.Empty()) throw new Exception("Duplicate Todo ID");
        Apply(new TodoItemCreated(id, title));
    }

    private void When(TodoItemCreated @event) {
        Id = @event.Id;
        Title = @event.Title;
    }
}
```
> For asynchronous snapshot creation, set SnapshotWhen to false, and handle snapshotting in a separate process. This approach can be useful in high-throughput scenarios. Setting it to null would use the `DefaultSnapshotThreshold` of 50.


### Loading and Saving the data through the Aggregate Repository

```csharp
var repository = builder.Services.GetRequiredService<AggregateRepository>();

var id = Guid.NewGuid();

// this part usually is unnecessary, but can be worthwhile in 
// some cases to check for conflicts
var todo = await repository.LoadAsync<Todo>(Todo.GetStreamId(id));
if (todo == null) {
    todo.Create(id, "Hello World");
    await repository.SaveAsync(todo);
}

```

### Processing Events

CosmosDB comes with a handy Azure Functions CosmosDBTrigger binding, a Change feed processor, and a Change feed pull model. Usage for the Change feed processor and change feed pull model can also be found in the example project.

The library provides a way to easily process events with the provided abstract class `EventProcessorBase` which does most of the heavy lifting. All you need to do is register the handlers for each event your processor needs to process.

```csharp
public TodoEventProjector : EventProcessorBase {
    private readonly AppState _state;

    public TodoEventProjector(AppState state) {
        _state = state;
        RegisterHandler<TodoItemCreated>(When);
        RegisterAsyncHandler<TodoItemCompleted>(When);
    }

    private void When(TodoItemCreated @event, EventWrapper<EventBase> context) {
        _state.Add(new TodoItem(@event.Id, @event.Title, context.version));
    }

    private async Task When(TodoItemCompleted @event, EventWrapper<EventBase> context, CancellationToken cancellationToken) {
        await _state.CompleteAsync(@event.Id, context.version);
    }
}
```

For Azure Functions CosmosDBTrigger binding, you're going to need to add a few lines of code on the `ConfigureFunctionsWorkerDefaults` on the `Program.cs` file to add the `EventWrapperConverter` so System.Text.Json would be able to deserialize the event for you.

>Program.cs
>```csharp
>var host = new HostBuilder()
>    .ConfigureFunctionsWorkerDefaults(builder => {
>        builder.Services.Configure<JsonSerializerOptions>(options => {
>            var converter = new EventWrapperConverter();
>            converter.RegisterDomainEvents("some-domain-name", [ typeof(SomeEvent), typeof(SomeOtherEvent) ]);
>            options.Converters.Add(converter);
>        });
>    })
>```


>YourCosmosDbTrigger.cs
>```csharp
> private readonly EventProjectorBase _projector;
>public YourCosmosDbTrigger() {
>    _projector = new TodoEventProjector();
>}
>
>[Function("YourCosmosDbTrigger")]
>public async Task Run([CosmosDbTrigger(/** parameters **/)] IReadOnlyList<EventWrapper<EventBase>> events) {
>    if (events is not { Count: > 0 }) return;
>
>    await _projector.ProcessAsync(events);
>}
>```

## What this library **WILL NOT** provide
- **An event publisher**. Initially created with CosmosDB in mind and considering the complexity 
of a reliable pub/sub framework; this is out of scope for the foreseeable future. 
For other providers lacking a change feed feature, alternatives like long polling your 
chosen data store and publishing to an event bus (e.g., Kafka, Azure EventHubs) are viable.

- **Event versioning (for now)**. Event schema changes are complex. Here's an opinion on handling schema changes:
    1. **Adding new properties**. Make new properties nullable. The domain's history dictates necessity.
    2. **Renaming/Removing properties**. Create a new event type with the changes. Republish old events to ensure downstream processing continuity.

  > Another approach involves streaming changes to a new event store with a translation layer, then transitioning services to the new store once it catches up.

## Future plans (aka What's Missing)

### Priorities at the moment

#### Create named AggregateRepositories

For cases when it's necessary for a service to handle multiple domains. (Still conjuring up a good way to set this up that's not too weird);

#### Per Aggregate Root snapshot provider target

For cases that one Aggregate Root snapshot might be stored in IDistributedCache and another in BlobStorage, for example.
This should make loading snapshots more flexible.

#### Primary and Fallback ISnapshotStores

For cases where snapshots are created on different locations, like the primary one in IDistributedStore with a short sliding TTL that is updated on the same process and another eventually consistent snapshot stored in a Blob Storage, for example.  

### In a not so distant future
- Add `ISnapshotStore` implementations for **CosmosDb**, **Azure Blob Storage**, and **File** (potentially more).
- Add `ILogger`s for everything.
- More comprehensive unit tests.
- Add proper exception handling.

## Looking for contributors
I've initially created this library out of necessity in the past. I needed a framework that is scalable and easy to maintain and that's when I learned about event sourcing. CosmosDB is relatively cheap and has an RU/s cap (especially now that they have a free tier of 1000 RU/s per month!). It has a way to save a document and the change feed acts like an atomic way of publishing events. Not to mention Azure Functions has change feed bindings.

I was already deep in development before I realized that there are already projects like Marten, or even EventStoreDB, so I just continued working on this. This library is the result of all my learning over the years and has worked well enough for my use-cases.

I know that not everybody is on Azure, or needs (/wants) their software hosted in the cloud - so I'm opening this up to contributors. I'm aware that AWS' DynamoDB has Change data capture, and MongoDB now has Change streams - but I have limited experience with both so, in case you're interested, don't hesitate sending me a PR!