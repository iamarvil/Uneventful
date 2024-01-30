## Basic Usage

The following code demonstrates basic usage of the library.

### Registering the `AggregateRepository` to Dependency Injection
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddEventStore(o => {
        // if you want to use the cosmos provider
        // o.UseCosmos(
        //    builder.Configuration.GetConnectionString("your-cosmos-db-connectionstring-key"),
        //    "cosmos-db-name",
        //    "cosmos-db-container"
        // );

        o.UseInMemory();

        o.WithSnapshotStore(snapshotStoreBuilder => {
            snapshotStoreBuilder.UseInMemory();
            snapshotStoreBuilder.DefaultSnapshotThreshold = 50;
        });
    });
```

### Domain Model Sample Aggregate

```csharp
public class Todo : EventSourced {
    public static string GetStreamId(Guid id) => $"todo:{id}";
    public override string StreamId => GetStreamId(Id);
    public Guid Id { get; private set; }
    public string? Title { get; private set; };

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

### Snapshot Capable Domain Model Sample Aggregate

The `ISnapshotCapable` interface allows the `AggregateRepository` to identify aggregates that support snapshotting. When implemented, the `AggregateRepository.SaveAsync` method checks the `SnapshotWhen` property to determine whether to create a snapshot. This is useful for optimizing the rehydration of aggregates by reducing the number of events that need to be replayed.

Here's an example of an aggregate that creates a snapshot every 50 changes:

```csharp
public class Todo : EventSourced, ISnapshotCapable {
    public static string GetStreamId(Guid id) => $"todo:{id}";
    public override string StreamId => GetStreamId(Id);
    public bool? SnapshotWhen => Version % 50; // snapshot every 50 versions
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
var repository = builder.Servies.GetRequiredService<AggregateRepository>();

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
>            options.Converters.Add(new EventWrapperConverter());
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