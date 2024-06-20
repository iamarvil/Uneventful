using TodoApp.Mongo.Domain.Events;
using TodoApp.Mongo.State;
using Uneventful.EventStore;
using static TodoApp.Mongo.Domain.Events.TodoEvents;

namespace TodoApp.Mongo.EventListener.Projectors;

internal class TodoEventProjector : EventProcessorBase {
    private readonly AppState _state;

    public TodoEventProjector(ILoggerFactory loggerFactory, AppState state) : base(loggerFactory) {
        _state = state;
        RegisterHandler<TodoItemCreated>(When);
        RegisterHandler<TodoItemTitleChanged>(When);
        RegisterHandler<TodoItemCompleted>(When);
        RegisterHandler<TodoItemUnCompleted>(When);
        RegisterHandler<TodoItemRemoved>(When);
    }

    private void When(TodoItemCreated @event, EventWrapper<EventBase> context) {
        var todo = _state.Get(@event.Id);
        if (todo != null) return;
        _state.Add(new TodoItem(@event.Id, @event.Title, context.Version));
    }
    
    private void When(TodoItemTitleChanged @event, EventWrapper<EventBase> context) {
        var todo = _state.Get(@event.Id);
        if (todo == null) {
            // todo: throw exception?
            return;
        } 
        if (todo.Version >= context.Version) return;
        _state.UpdateTitle(@event.Id, @event.Title, context.Version);
    }
    
    private void When(TodoItemCompleted @event, EventWrapper<EventBase> context) {
        var todo = _state.Get(@event.Id);
        if (todo == null) {
            // todo: throw exception?
            return;
        } 
        if (todo.Version >= context.Version) return;
        _state.Complete(@event.Id, context.Version);
    }

    private void When(TodoItemUnCompleted @event, EventWrapper<EventBase> context) {
        var todo = _state.Get(@event.Id);
        if (todo == null) {
            // todo: throw exception?
            return;
        } 
        if (todo.Version >= context.Version) return;
        _state.MarkUncompleted(@event.Id, context.Version);
    }

    private void When(TodoItemRemoved @event, EventWrapper<EventBase> context) {
        var todo = _state.Get(@event.Id);
        if (todo == null) {
            // todo: throw exception?
            return;
        } 
        if (todo.Version >= context.Version) return;
        _state.Remove(@event.Id);
    }
}