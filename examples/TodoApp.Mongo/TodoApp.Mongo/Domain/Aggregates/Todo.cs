using Uneventful.Repository;
using Uneventful.Repository.Snapshot;
using static TodoApp.Mongo.Domain.Events.TodoEvents;

namespace TodoApp.Mongo.Domain.Aggregates;

public class Todo : EventSourced, ISnapshotCapable {
    
    public static string GetStreamId(Guid id) => $"todo:{id}";
    public override string StreamId => GetStreamId(Id);
    public bool? SnapshotWhen => (Version > 0 && Version % 50 == 0) || IsCompleted || IsRemoved;
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public long CreatedOn { get; private set; }
    public bool IsRemoved { get; private set; }

    public Todo() {
        RegisterHandler<TodoItemCreated>(When);
        RegisterHandler<TodoItemTitleChanged>(When);
        RegisterHandler<TodoItemCompleted>(When);
        RegisterHandler<TodoItemUnCompleted>(When);
        RegisterHandler<TodoItemRemoved>(When);
    }

    public static Todo Create(Guid id, string? title) {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty.", nameof(title));
        
        var todo = new Todo();
        todo.Apply(new TodoItemCreated(id, title, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        return todo;
    }
    
    public void ChangeTitle(string title) {
        if (Id == Guid.Empty) throw new InvalidOperationException("Todo item is not found.");
        if (title == Title || IsRemoved) return;
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty.", nameof(title));
        
        Apply(new TodoItemTitleChanged(Id, title, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }
    
    public void Complete() {
        if (Id == Guid.Empty) throw new InvalidOperationException("Todo item is not found.");
        if (IsCompleted || IsRemoved) return;
        
        Apply(new TodoItemCompleted(Id, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }
    
    public void UnComplete() {
        if (Id == Guid.Empty) throw new InvalidOperationException("Todo item is not found.");
        if (!IsCompleted || IsRemoved) return;
        
        Apply(new TodoItemUnCompleted(Id, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }
    
    public void Remove() {
        if (Id == Guid.Empty) throw new InvalidOperationException("Todo item is not found.");
        if (IsRemoved) return;
        
        Apply(new TodoItemRemoved(Id, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }

    private void When(TodoItemCreated @event) {
        Id = @event.Id;
        Title = @event.Title;
        CreatedOn = @event.On;
    }
    
    private void When(TodoItemTitleChanged @event) {
        Title = @event.Title;
    }
    
    private void When(TodoItemCompleted @event) {
        IsCompleted = true;
    }
    
    private void When(TodoItemUnCompleted @event) {
        IsCompleted = false;
    }

    private void When(TodoItemRemoved @event) {
        IsRemoved = true;
    }
}

