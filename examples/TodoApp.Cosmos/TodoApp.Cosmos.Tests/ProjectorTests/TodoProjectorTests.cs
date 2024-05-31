using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TodoApp.Cosmos.EventListener.Projectors;
using TodoApp.Cosmos.State;
using Uneventful.EventStore;
using static ToDoApp.Cosmos.Domain.Events.TodoEvents;

namespace TodoApp.Cosmos.Tests.ProjectorTests;

public class TodoProjectorTests {
    private readonly EventProcessorBase _projector;
    private readonly AppState _appState;

    public TodoProjectorTests() {
        ILoggerFactory loggerFactory = new NullLoggerFactory();
        _appState = new AppState();
        _projector = new TodoEventProjector(loggerFactory, _appState);
    }
    
    [Fact]
    public async Task NoEventsDoesNothing() {
        await _projector.ProcessAsync([]);
        
        Assert.Empty(_appState.TodoItems);
    }
    
    [Fact]
    public async Task TodoCreatedEventAddsTodoItemToAppState() {
        var id = Guid.NewGuid();
        
        await _projector.ProcessAsync([
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemCreated), new TodoItemCreated(id, "Test Todo", 1), 1, 1),
            // doubling for idempotency check
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemCreated), new TodoItemCreated(id, "Test Todo", 1), 1, 1)
        ]);
        
        Assert.Single(_appState.TodoItems);
        Assert.Equal(id, _appState.TodoItems[0].Id);
        Assert.Equal("Test Todo", _appState.TodoItems[0].Title);
        Assert.Equal(1, _appState.TodoItems[0].Version);
    }
    
    [Fact]
    public async Task TodoTitleChangedEventUpdatesTodoItemInAppState() {
        var id = Guid.NewGuid();
        _appState.Add(new TodoItem(id, "Test Todo", 1));
        
        await _projector.ProcessAsync([
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemTitleChanged), new TodoItemTitleChanged(id, "Test Todo 2", 2), 2, 2),
            // doubling for idempotency check
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemTitleChanged), new TodoItemTitleChanged(id, "Test Todo 2", 2), 2, 2)
        ]);
        
        Assert.Single(_appState.TodoItems);
        Assert.Equal(id, _appState.TodoItems[0].Id);
        Assert.Equal("Test Todo 2", _appState.TodoItems[0].Title);
        Assert.Equal(2, _appState.TodoItems[0].Version);
    }
    
    [Fact]
    public async Task TodoCompletedEventUpdatesTodoItemInAppState() {
        var id = Guid.NewGuid();
        _appState.Add(new TodoItem(id, "Test Todo", 1));
        
        await _projector.ProcessAsync([
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemCompleted), new TodoItemCompleted(id, 2), 2, 2),
            // doubling for idempotency check
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemCompleted), new TodoItemCompleted(id, 2), 2, 2),
        ]);
        
        Assert.Single(_appState.TodoItems);
        Assert.Equal(id, _appState.TodoItems[0].Id);
        Assert.True(_appState.TodoItems[0].IsCompleted);
        Assert.Equal(2, _appState.TodoItems[0].Version);
    }
    
    [Fact]
    public async Task TodoUnCompletedEventUpdatesTodoItemInAppState() {
        var id = Guid.NewGuid();
        _appState.Add(new TodoItem(id, "Test Todo", 1));
        _appState.Complete(id, 1);
        
        await _projector.ProcessAsync([
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemUnCompleted), new TodoItemUnCompleted(id, 2), 2, 2),
            // doubling for idempotency check
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemUnCompleted), new TodoItemUnCompleted(id, 2), 2, 2)
        ]);
        
        Assert.Single(_appState.TodoItems);
        Assert.Equal(id, _appState.TodoItems[0].Id);
        Assert.False(_appState.TodoItems[0].IsCompleted);
        Assert.Equal(2, _appState.TodoItems[0].Version);
    }
    
    [Fact]
    public async Task TodoRemovedEventRemovesTodoItemFromAppState() {
        var id = Guid.NewGuid();
        _appState.Add(new TodoItem(id, "Test Todo", 1));
        
        await _projector.ProcessAsync([
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemRemoved), new TodoItemRemoved(id, 2), 2, 2),
            // doubling for idempotency check
            new EventWrapper<EventBase>($"todo:{id}",nameof(TodoItemRemoved), new TodoItemRemoved(id, 2), 2, 2)
        ]);
        
        Assert.Empty(_appState.TodoItems);
    }
}