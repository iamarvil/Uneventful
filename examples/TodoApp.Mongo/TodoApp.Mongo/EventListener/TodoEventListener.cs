using MongoDB.Bson;
using MongoDB.Driver;
using TodoApp.Mongo.EventListener.Projectors;
using TodoApp.Mongo.State;
using Uneventful.EventStore;
using Uneventful.EventStore.Mongo;

namespace TodoApp.Mongo.EventListener;

internal class TodoEventListener : BackgroundService {
    private readonly IMongoCollection<EventWrapper<EventBase>> _eventCollection;
    private readonly PipelineDefinition<ChangeStreamDocument<EventWrapper<EventBase>>, ChangeStreamDocument<EventWrapper<EventBase>>> _pipeline;
    private IChangeStreamCursor<ChangeStreamDocument<EventWrapper<EventBase>>>? _cursor;
    private readonly TodoEventProjector _eventProjector;
    private readonly ILogger<TodoEventListener> _logger;

    public TodoEventListener(IEventStore eventStore, AppState appState, ILoggerFactory loggerFactory) {
        var eventCollection = (eventStore as MongoEventStore)?.Collection;
        _eventCollection = eventCollection ?? throw new InvalidOperationException("EventStore must be a MongoEventStore");
        _pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<EventWrapper<EventBase>>>()
            .Match(change => change.OperationType == ChangeStreamOperationType.Insert);

        _logger = loggerFactory.CreateLogger<TodoEventListener>();
        _eventProjector = new TodoEventProjector(loggerFactory, appState);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (_cursor == null) {
            throw new InvalidOperationException("Cursor must be initialized before starting the event listener.");
        }
        
        _logger.LogInformation("Starting event listener.");
        while (await _cursor.MoveNextAsync(stoppingToken)) {
            if (!_cursor.Current.Any()) {
                continue;
            }
            

            try {

                var events = _cursor.Current.Select(x => x.FullDocument).ToArray();
                await _eventProjector.ProcessAsync(events, stoppingToken);
                
                _logger.LogInformation("Processed {Count} events.", events.Length);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error processing events.");
            }
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken) {
        _cursor = await _eventCollection.WatchAsync(
            _pipeline, 
            new ChangeStreamOptions {
                StartAtOperationTime = new BsonTimestamp(0, 0),
                BatchSize = 100,
            },
            cancellationToken: cancellationToken
        );

        await base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        _cursor?.Dispose();
        return Task.CompletedTask;
    }
}