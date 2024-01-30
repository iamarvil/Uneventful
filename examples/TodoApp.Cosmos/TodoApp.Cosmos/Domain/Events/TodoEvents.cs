using Uneventful.EventStore;

namespace ToDoApp.Cosmos.Domain.Events;

public class TodoItemCreated : EventBase {
    public Guid Id { get; }
    public string Title { get; }
    public long On { get; }

    public TodoItemCreated(Guid id, string title, long on) {
        Id = id;
        Title = title;
        On = on;
    }
}

public class TodoItemTitleChanged : EventBase {
    public Guid Id { get; }
    public string Title { get; }
    public long On { get; }

    public TodoItemTitleChanged(
        Guid id,
        string title,
        long on
    ) {
        Id = id;
        Title = title;
        On = on;
    }
}

public class TodoItemCompleted : EventBase {
    public Guid Id { get; }
    public long On { get; }

    public TodoItemCompleted(
        Guid id,
        long on
    ) {
        Id = id;
        On = on;
    }
}

public class TodoItemUnCompleted : EventBase {
    public Guid Id { get; }
    public long On { get; }

    public TodoItemUnCompleted(
        Guid id,
        long on
    ) {
        Id = id;
        On = on;
    }
}

public class TodoItemRemoved : EventBase {
    public Guid Id { get; }
    public long On { get; }

    public TodoItemRemoved(
        Guid id,
        long on
    ) {
        Id = id;
        On = on;
    }
}

