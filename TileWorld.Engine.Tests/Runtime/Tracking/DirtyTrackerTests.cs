using TileWorld.Engine.Runtime.Tracking;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime.Tracking;

public sealed class DirtyTrackerTests
{
    [Fact]
    public void MarkAndClearDirty_WorksForSpecificFlags()
    {
        var worldData = new WorldData(new WorldMetadata());
        var dirtyTracker = new DirtyTracker(worldData);

        dirtyTracker.MarkRenderDirty(new ChunkCoord(0, 0));
        dirtyTracker.MarkSaveDirty(new ChunkCoord(0, 0));

        Assert.True(dirtyTracker.HasDirty(new ChunkCoord(0, 0), ChunkDirtyFlags.RenderDirty));
        Assert.True(dirtyTracker.HasDirty(new ChunkCoord(0, 0), ChunkDirtyFlags.SaveDirty));

        dirtyTracker.ClearDirty(new ChunkCoord(0, 0), ChunkDirtyFlags.RenderDirty);

        Assert.False(dirtyTracker.HasDirty(new ChunkCoord(0, 0), ChunkDirtyFlags.RenderDirty));
        Assert.True(dirtyTracker.HasDirty(new ChunkCoord(0, 0), ChunkDirtyFlags.SaveDirty));
    }

    [Fact]
    public void EnumerateDirtyChunks_FiltersByRequestedFlags()
    {
        var worldData = new WorldData(new WorldMetadata());
        var dirtyTracker = new DirtyTracker(worldData);

        dirtyTracker.MarkRenderDirty(new ChunkCoord(0, 0));
        dirtyTracker.MarkCollisionDirty(new ChunkCoord(1, 0));

        var renderDirtyChunks = dirtyTracker.EnumerateDirtyChunks(ChunkDirtyFlags.RenderDirty).ToArray();

        Assert.Single(renderDirtyChunks);
        Assert.Equal(new ChunkCoord(0, 0), renderDirtyChunks[0]);
    }

    [Fact]
    public void MarkNeighborDirtyIfBoundary_OnlyTouchesCardinalNeighbors()
    {
        var worldData = new WorldData(new WorldMetadata());
        var dirtyTracker = new DirtyTracker(worldData);
        worldData.GetOrCreateChunk(new ChunkCoord(-1, 0));
        worldData.GetOrCreateChunk(new ChunkCoord(1, 0));
        worldData.GetOrCreateChunk(new ChunkCoord(0, -1));
        worldData.GetOrCreateChunk(new ChunkCoord(0, 1));
        worldData.GetOrCreateChunk(new ChunkCoord(1, 1));

        dirtyTracker.MarkNeighborDirtyIfBoundary(new WorldTileCoord(31, 31), ChunkDirtyFlags.RenderDirty);

        Assert.True(dirtyTracker.HasDirty(new ChunkCoord(1, 0), ChunkDirtyFlags.RenderDirty));
        Assert.True(dirtyTracker.HasDirty(new ChunkCoord(0, 1), ChunkDirtyFlags.RenderDirty));
        Assert.False(dirtyTracker.HasDirty(new ChunkCoord(1, 1), ChunkDirtyFlags.RenderDirty));
    }
}
