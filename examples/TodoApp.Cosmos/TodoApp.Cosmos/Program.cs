using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TodoApp.Cosmos;
using TodoApp.Cosmos.Endpoints.TodoEndpoints;
using TodoApp.Cosmos.EventListener;
using TodoApp.Cosmos.Options;
using TodoApp.Cosmos.State;
using TodoApp.Cosmos.Utilities;
using Uneventful.EventStore;
using Uneventful.EventStore.Cosmos;
using Uneventful.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<AppState>();

builder.Services.AddOptions<CosmosOptions>()
    .Bind(builder.Configuration.GetSection(CosmosOptions.CosmosConfig))
    .Validate(config => !config.NoEventStoreConnectionString,
        "Connection string or account endpoint and key for EventStore must be provided.")
    .Validate(config => !config.NoEventStoreDatabaseNameAndContainerName,
        "Database name and container name for EventStore must be provided.")
    .Validate(config => !config.NoLeasesConnectionString,
        "Connection string or account endpoint and key for leases must be provided.")
    .Validate(config => !config.NoLeasesDatabaseNameAndContainerName,
        "Database name and container name for leases must be provided.");

builder.Services.AddMemoryCache();

builder.Services
    .AddEventStore(
        Constants.DomainName,
        (eventStoreBuilder) => {
            var eventStoreConfig = builder.Configuration.GetSection(CosmosOptions.CosmosEventStoreConfig);
            eventStoreBuilder
                .RegisterTodoAppCosmosEvents(Constants.DomainName)
                .UseCosmos(
                    eventStoreConfig.GetSection("AccountEndPoint").Get<string>()!,
                    eventStoreConfig.GetSection("AccountKey").Get<string>()!,
                    eventStoreConfig.GetSection("DatabaseName").Get<string>()!,
                    eventStoreConfig.GetSection("ContainerName").Get<string>()!
                );
        }
    )
    .AddAggregateRepository();

builder.Services
    .AddSingleton<CosmosClient>(s => {
        var eventStore = s.GetRequiredService<IEventStore>();
        return eventStore.GetCosmosEventStore().Client;
    })
    .AddSingleton<SetupCosmosDb>(s => {
        var cosmosOptions = s.GetRequiredService<IOptions<CosmosOptions>>();
        var cosmosClient = s.GetRequiredService<CosmosClient>();

        return new SetupCosmosDb(cosmosClient, cosmosOptions);
    })
    .AddSingleton<TodoEventListener>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseTodoEndpoints();

// create the cosmos db database and container if they don't exist
var cosmosCreator = app.Services.GetService<SetupCosmosDb>();
if (cosmosCreator != null) {
    await cosmosCreator.RunAsync();
}

// event processor code. usually this would be in a separate service
// this also rebuilds the app state on startup
var todoEventListener = app.Services.GetService<TodoEventListener>();
if (todoEventListener != null) {
    await todoEventListener.RestoreStateAndStartListening();
}

app.Run();