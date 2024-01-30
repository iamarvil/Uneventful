namespace Uneventful.EventStore.Exceptions;

public class EventStoreWriteConflictException : Exception {
    public EventStoreWriteConflictException(string message) : base(message) {
    }

    public EventStoreWriteConflictException(string message, Exception? innerException) : base(message, innerException) {
    }
}