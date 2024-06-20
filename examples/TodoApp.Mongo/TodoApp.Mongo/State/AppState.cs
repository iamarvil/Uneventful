namespace TodoApp.Mongo.State;

internal class AppState {
    private TodoItem[] _todoItems = [];

    public TodoItem[] TodoItems => _todoItems;

    public void Add(TodoItem todoItem) {
        _todoItems = _todoItems.Append(todoItem).ToArray();
    }
    
    public TodoItem? Get(Guid id) {
        return _todoItems.FirstOrDefault(todoItem => todoItem.Id == id);
    }
    
    public void Remove(Guid id) {
        _todoItems = _todoItems.Where(todoItem => todoItem.Id != id).ToArray();
    }
    
    public void UpdateTitle(Guid id, string title, long version) {
        _todoItems = _todoItems
            .Select(x => x.Id == id ? x.CopyWith(title: title, version: version) : x)
            .ToArray();
    }
    
    public void Complete(Guid id, long version) {
        _todoItems = _todoItems
            .Select(x => x.Id == id ? x.CopyWith(isCompleted: true, version: version) : x)
            .ToArray();
    }
    
    public void MarkUncompleted(Guid id, long version) {
        _todoItems = _todoItems
            .Select(x => x.Id == id ? x.CopyWith(isCompleted: false, version: version) : x)
            .ToArray();
    }
}

public class TodoItem {
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public long Version { get; private set; }
    public bool IsCompleted { get; private set; }

    public TodoItem(Guid id, string title, long version) {
        Id = id;
        Title = title;
        Version = version;
    }

    private TodoItem(Guid id, string title, bool isCompleted, long version) {
        Id = id;
        Title = title;
        IsCompleted = isCompleted;
        Version = version;
    }
    
    public TodoItem CopyWith(Guid? id = null, string? title = null, bool? isCompleted = null, long? version = null) {
        return new TodoItem(
            id ?? Id, 
            title ?? Title, 
            isCompleted ?? IsCompleted,
            version ?? Version
        );
    }
}