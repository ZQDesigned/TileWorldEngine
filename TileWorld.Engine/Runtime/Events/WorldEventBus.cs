using System;
using System.Collections.Generic;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Provides a synchronous strongly typed event bus for world runtime notifications.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should subscribe through <see cref="Runtime.WorldRuntime"/>
/// rather than depending on the raw event bus implementation.
/// </remarks>
internal sealed class WorldEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscriptions = new();

    /// <summary>
    /// Publishes an event to all currently subscribed handlers of the same payload type.
    /// </summary>
    /// <typeparam name="TEvent">The event payload type.</typeparam>
    /// <param name="evt">The event payload to publish.</param>
    public void Publish<TEvent>(TEvent evt)
    {
        if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
        {
            return;
        }

        var snapshot = handlers.ToArray();
        foreach (var handler in snapshot)
        {
            ((Action<TEvent>)handler).Invoke(evt);
        }
    }

    /// <summary>
    /// Registers an event handler for a specific payload type.
    /// </summary>
    /// <typeparam name="TEvent">The event payload type.</typeparam>
    /// <param name="handler">The handler to invoke when matching events are published.</param>
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
        {
            handlers = new List<Delegate>();
            _subscriptions.Add(typeof(TEvent), handlers);
        }

        handlers.Add(handler);
    }

    /// <summary>
    /// Removes a previously registered event handler.
    /// </summary>
    /// <typeparam name="TEvent">The event payload type.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler is null)
        {
            return;
        }

        if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
        {
            return;
        }

        handlers.Remove(handler);
        if (handlers.Count == 0)
        {
            _subscriptions.Remove(typeof(TEvent));
        }
    }
}
