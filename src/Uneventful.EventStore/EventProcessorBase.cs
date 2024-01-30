using Microsoft.Extensions.Logging;

namespace Uneventful.EventStore;

public abstract class EventProcessorBase {
    private readonly HashSet<string> _eventTypes = []; 
    private readonly Dictionary<string, Func<EventBase, EventWrapper<EventBase>, CancellationToken, Task>> _asyncEventHandlers = new ();
    private readonly Dictionary<string, Action<EventBase, EventWrapper<EventBase>>> _eventHandlers = new();
    private readonly Type _processorType;
    protected readonly ILogger? Logger;

    protected EventProcessorBase(ILoggerFactory? loggerFactory = null) {
        _processorType = GetType();
        Logger = loggerFactory?.CreateLogger<EventProcessorBase>();
    }
    
    /// <summary>
    /// Registers an asynchronous handler for processing events of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the event. This type must inherit from <see cref="EventBase"/>.</typeparam>
    /// <param name="handler">The asynchronous handler function to process the event.
    /// The function takes three parameters: a string representing the streamId,
    /// the payload instance of <typeparamref name="T"/> representing the event to be handled, and a long representing 
    /// the event version.</param>
    /// <remarks>
    /// This method allows for custom event handling logic to be defined for events of type <typeparamref name="T"/>.
    /// The handler provided should contain the logic for processing the event, including any necessary asynchronous
    /// operations.
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
    /// RegisterAsyncHandler&lt;SomeEvent&gt;(async (eventId, @event, version) => {
    ///     // Your handling logic here
    /// });
    /// </code>
    /// </example>
    protected void RegisterAsyncHandler<T>(Func<T, EventWrapper<EventBase>, CancellationToken, Task> handler) where T : EventBase {
        var type = typeof(T);
        _asyncEventHandlers.Add(type.Name, (a, b, c) => handler((T)a, b, c));
        _eventTypes.Add(type.Name);
    }
    
    /// <summary>
    /// Registers a handler for processing events of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the event. This type must inherit from <see cref="EventBase"/>.</typeparam>
    /// <param name="handler">The asynchronous handler function to process the event.
    /// The function takes three parameters: a string representing the streamId,
    /// the payload instance of <typeparamref name="T"/> representing the event to be handled, and a long representing 
    /// the event version.</param>
    /// <remarks>
    /// This method allows for custom event handling logic to be defined for events of type <typeparamref name="T"/>.
    /// The handler provided should contain the logic for processing the event.
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
    /// RegisterHandler&lt;SomeEvent&gt;((eventId, @event, version) =>
    /// {
    ///     // Your handling logic here
    /// });
    /// </code>
    /// </example>
    protected void RegisterHandler<T>(Action<T, EventWrapper<EventBase>> handler) where T : EventBase {
        var type = typeof(T);
        _eventHandlers.Add(type.Name, (a, b) => handler((T)a, b));
        _eventTypes.Add(type.Name);
    }
    
    /// <summary>
    /// Registers an event type to be processed by this event processor. Useful for times that a processor overrides
    /// the ProcessAsync method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    protected void RegisterType<T>() where T : EventBase {
        var type = typeof(T);
        _eventTypes.Add(type.Name);
    }
    
    /// <summary>
    /// Will only apply an event registered with an async handler
    /// </summary>
    /// <param name="eventWrapper"></param>
    /// <param name="cancellationToken"></param>
    public virtual async Task ApplyAsync(EventWrapper<EventBase> eventWrapper, CancellationToken cancellationToken = default) { 
        if (eventWrapper.Payload is not { } payload) return;
        if (!_asyncEventHandlers.TryGetValue(eventWrapper.EventType, out Func<EventBase, EventWrapper<EventBase>, CancellationToken, Task>? handler)) {
            Logger?.LogWarning("No handler registered for \"{eventType}\" on \"{processor}\"", eventWrapper.EventType, _processorType.Name);
            return;
        }

        Logger?.LogDebug("Applying {eventId} of \"{eventType}\" on \"{processor}\"", eventWrapper.Id, eventWrapper.EventType, _processorType.Name);
        await handler
            .Invoke(
                payload,
                eventWrapper,
                cancellationToken
            );
    }
    
    /// <summary>
    /// Will only apply an event registered with a non-async handler
    /// </summary>
    /// <param name="eventWrapper"></param>
    public virtual void Apply(EventWrapper<EventBase> eventWrapper) {
        if (eventWrapper.Payload is not { } payload) return;
        if (!_eventHandlers.TryGetValue(eventWrapper.EventType, out Action<EventBase, EventWrapper<EventBase>>? handler)) {
            Logger?.LogWarning("No handler registered for \"{eventType}\" on \"{processor}\"", eventWrapper.EventType, _processorType.Name);
            return;
        }

        Logger?.LogDebug("Applying {eventId} of \"{eventType}\" on \"{processor}\"", eventWrapper.Id, eventWrapper.EventType, _processorType.Name);
        handler.Invoke(
                payload,
                eventWrapper
            );
    }
    
    /// <summary>
    /// Will process all events registered with either async or non-async handlers
    /// </summary>
    /// <param name="eventWrappers"></param>
    /// <param name="cancellationToken"></param>
    public virtual async Task ProcessAsync(IEnumerable<EventWrapper<EventBase>> eventWrappers, CancellationToken cancellationToken = default) {
        foreach (var eventWrapper in eventWrappers.Where(x => _eventTypes.Contains(x.EventType))) {
            try {
                if (_asyncEventHandlers.ContainsKey(eventWrapper.EventType)) await ApplyAsync(eventWrapper, cancellationToken);
                else if (_eventHandlers.ContainsKey(eventWrapper.EventType)) Apply(eventWrapper);
            } catch (Exception ex) {
                Logger?.LogError(ex, "Error when applying {eventId} of \"{eventType}\" on \"{processor}\"", eventWrapper.Id, eventWrapper.EventType, _processorType.Name);
                // todo: add a processor exception of sorts
                throw;
            }
        }
    }

}