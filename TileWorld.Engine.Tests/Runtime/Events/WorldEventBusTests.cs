using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime.Events;

public sealed class WorldEventBusTests
{
    [Fact]
    public void Publish_InvokesSubscribedHandlers()
    {
        var eventBus = new WorldEventBus();
        var seenEvents = new List<TileChangedEvent>();

        eventBus.Subscribe<TileChangedEvent>(seenEvents.Add);
        eventBus.Publish(new TileChangedEvent(new WorldTileCoord(1, 2), 3, 4));

        Assert.Single(seenEvents);
    }

    [Fact]
    public void Unsubscribe_RemovesHandler()
    {
        var eventBus = new WorldEventBus();
        var callCount = 0;

        void Handler(TileChangedEvent evt) => callCount++;

        eventBus.Subscribe<TileChangedEvent>(Handler);
        eventBus.Unsubscribe<TileChangedEvent>(Handler);
        eventBus.Publish(new TileChangedEvent(new WorldTileCoord(0, 0), 0, 1));

        Assert.Equal(0, callCount);
    }
}
