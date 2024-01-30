using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TodoApp.Cosmos.EventListener.Projectors;
using TodoApp.Cosmos.Options;
using TodoApp.Cosmos.State;
using Uneventful.EventStore;

namespace TodoApp.Cosmos.EventListener;

internal class TodoEventListener {
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosOptions _options;
    private readonly ILogger<TodoEventListener> _logger;
    private readonly TodoEventProjector _projector;

    public TodoEventListener(ILoggerFactory loggerFactory, CosmosClient cosmosClient, IOptions<CosmosOptions> options, AppState appState) {
        _cosmosClient = cosmosClient;
        _options = options.Value;
        _logger = loggerFactory.CreateLogger<TodoEventListener>();
        _projector = new TodoEventProjector(loggerFactory, appState);
    }
    
    public async Task<ChangeFeedProcessor> RestoreStateAndStartListening() {
        var database = _cosmosClient.GetDatabase(_options.EventStore.DatabaseName);
        var container = database.GetContainer(_options.EventStore.ContainerName);
        
        await foreach(var @event in GetHistory(container)) {
            _projector.Apply(@event);
        }
        
        var leaseContainer = _cosmosClient.GetContainer(_options.Leases.DatabaseName, _options.Leases.ContainerName);
        var processor = container.GetChangeFeedProcessorBuilder<EventWrapper<EventBase>>(
                "TodoEventListener", 
                (
                    _, changes, cancellationToken
                ) => HandleChangesAsync(_projector, changes, cancellationToken)
            )
            .WithInstanceName("consoleHost")
            .WithLeaseContainer(leaseContainer)
            .WithStartTime(DateTime.MinValue.ToUniversalTime())
            .WithLeaseAcquireNotification(x => {
                _logger.LogInformation("Lease acquired with lease {lease}", x);
                return Task.CompletedTask;
            })
            .WithLeaseReleaseNotification(x => {
                _logger.LogInformation("Lease {lease} released", x);
                return Task.CompletedTask;
            })
            .WithErrorNotification((token, exception) => {
                _logger.LogError(exception, "An error occurred in the change feed processor");
                return Task.CompletedTask;
            })
            .Build();
        
        await processor.StartAsync();
        _logger.LogInformation("Change feed processor started.");
        return processor;
    }

    private static async IAsyncEnumerable<EventWrapper<EventBase>> GetHistory(Container container) {
        using var iterator = container.GetChangeFeedIterator<EventWrapper<EventBase>>(ChangeFeedStartFrom.Beginning(), ChangeFeedMode.Incremental);

        while (iterator.HasMoreResults) {
            var response = await iterator.ReadNextAsync();
            if (response.StatusCode == HttpStatusCode.NotModified) {
                break;
            }
            
            foreach (var item in response) {
                yield return item;
            }
        }
    }

    private static async Task HandleChangesAsync(TodoEventProjector projector, IReadOnlyCollection<EventWrapper<EventBase>> changes, CancellationToken cancellationToken) {
        await projector.ProcessAsync(changes, cancellationToken);
    }
}