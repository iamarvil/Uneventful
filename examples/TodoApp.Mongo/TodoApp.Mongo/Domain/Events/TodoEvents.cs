using Uneventful.EventStore;

namespace TodoApp.Mongo.Domain.Events;

public static class TodoEvents {
    public record TodoItemCreated : EventBase {
        public Guid Id { get; }
        public string Title { get; }
        public long On { get; }

        public TodoItemCreated(Guid id, string title, long on) {
            Id = id;
            Title = title;
            On = on;
        }
    }

    public record TodoItemTitleChanged : EventBase {
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

    public record TodoItemCompleted : EventBase {
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

    public record TodoItemUnCompleted : EventBase {
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

    public record TodoItemRemoved : EventBase {
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

}