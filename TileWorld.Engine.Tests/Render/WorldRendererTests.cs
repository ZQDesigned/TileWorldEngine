using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Render;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Render;

public sealed class WorldRendererTests
{
    [Fact]
    public void GetVisibleChunkCoords_ForPositiveCameraReturnsStableOrder()
    {
        var renderer = CreateRenderer(new Camera2D(Int2.Zero, new Int2(512, 512)));

        var visibleCoords = renderer.GetVisibleChunkCoords().ToArray();

        Assert.Equal(9, visibleCoords.Length);
        Assert.Equal(new ChunkCoord(-1, -1), visibleCoords[0]);
        Assert.Equal(new ChunkCoord(1, -1), visibleCoords[2]);
        Assert.Equal(new ChunkCoord(-1, 1), visibleCoords[6]);
        Assert.Equal(new ChunkCoord(1, 1), visibleCoords[8]);
    }

    [Fact]
    public void GetVisibleChunkCoords_ForNegativeCameraUsesFloorDivision()
    {
        var renderer = CreateRenderer(new Camera2D(new Int2(-32, -32), new Int2(32, 32)));

        var visibleCoords = renderer.GetVisibleChunkCoords().ToArray();

        Assert.Equal(9, visibleCoords.Length);
        Assert.Equal(new ChunkCoord(-2, -2), visibleCoords[0]);
        Assert.Equal(new ChunkCoord(0, 0), visibleCoords[8]);
    }

    [Fact]
    public void RebuildDirtyCaches_RebuildsOnlyRenderDirtyAndPreservesOtherFlags()
    {
        var runtime = CreateRuntime();
        var renderer = CreateRenderer(new Camera2D(Int2.Zero, new Int2(512, 512)), runtime.ContentRegistry);

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        var chunkCoord = new ChunkCoord(0, 0);
        Assert.True(runtime.DirtyTracker.HasDirty(chunkCoord, ChunkDirtyFlags.RenderDirty));
        Assert.True(runtime.DirtyTracker.HasDirty(chunkCoord, ChunkDirtyFlags.SaveDirty));

        renderer.RebuildDirtyCaches(runtime);

        Assert.True(renderer.TryGetChunkRenderCache(chunkCoord, out var cache));
        Assert.True(cache.IsBuilt);
        Assert.False(runtime.DirtyTracker.HasDirty(chunkCoord, ChunkDirtyFlags.RenderDirty));
        Assert.True(runtime.DirtyTracker.HasDirty(chunkCoord, ChunkDirtyFlags.SaveDirty));
    }

    [Fact]
    public void RebuildDirtyCaches_DoesNotRebuildWhenOnlyCameraMoves()
    {
        var runtime = CreateRuntime();
        var camera = new Camera2D(Int2.Zero, new Int2(512, 512));
        var renderer = CreateRenderer(camera, runtime.ContentRegistry);

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        renderer.RebuildDirtyCaches(runtime);
        var firstBuildTick = renderer.TryGetChunkRenderCache(new ChunkCoord(0, 0), out var cache)
            ? cache.LastBuildTick
            : -1;

        camera.PositionPixels = new Int2(128, 64);
        renderer.RebuildDirtyCaches(runtime);

        Assert.True(renderer.TryGetChunkRenderCache(new ChunkCoord(0, 0), out cache));
        Assert.Equal(firstBuildTick, cache.LastBuildTick);
    }

    [Fact]
    public void Draw_SubmitsOnlyVisibleChunkCommandsWithScreenSpaceOffset()
    {
        var runtime = CreateRuntime();
        var camera = new Camera2D(new Int2(16, 8), new Int2(512, 512));
        var renderer = CreateRenderer(camera, runtime.ContentRegistry);
        var renderContext = new FakeRenderContext(camera.ViewportSizePixels);

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(1, 1),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });
        runtime.PlaceTile(
            new WorldTileCoord(100, 1),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        renderer.RebuildDirtyCaches(runtime);
        renderer.Draw(runtime, renderContext);

        var command = Assert.Single(renderContext.DrawCalls);
        Assert.Equal(new RectI(0, 8, 16, 16), command.DestinationRectPixels);
    }

    [Fact]
    public void RebuildDirtyCaches_UsesFrameBudgetAndPrioritizesVisibleChunks()
    {
        var runtime = CreateRuntime();
        var camera = new Camera2D(Int2.Zero, new Int2(512, 512));
        var settings = new WorldRenderSettings(tileSizePixels: 16, visibleChunkPadding: 1, maxDirtyChunkCacheRebuildsPerFrame: 1);
        var renderer = new WorldRenderer(camera, new ChunkRenderCacheBuilder(runtime.ContentRegistry, settings), settings);

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });
        runtime.PlaceTile(
            new WorldTileCoord(320, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        renderer.RebuildDirtyCaches(runtime);

        Assert.True(renderer.TryGetChunkRenderCache(new ChunkCoord(0, 0), out _));
        Assert.False(renderer.TryGetChunkRenderCache(new ChunkCoord(10, 0), out _));
        Assert.False(runtime.DirtyTracker.HasDirty(new ChunkCoord(0, 0), ChunkDirtyFlags.RenderDirty));
        Assert.True(runtime.DirtyTracker.HasDirty(new ChunkCoord(10, 0), ChunkDirtyFlags.RenderDirty));
    }

    private static WorldRuntime CreateRuntime()
    {
        var registry = new ContentRegistry();
        registry.RegisterTile(new TileDef
        {
            Id = 1,
            Name = "Stone",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1,
            AutoTileGroupId = 1,
            Visual = new TileVisualDef(
                "debug/white",
                new RectI(0, 0, 1, 1),
                new ColorRgba32(130, 130, 130),
                false)
        });

        return new WorldRuntime(new WorldData(new WorldMetadata()), registry);
    }

    private static WorldRenderer CreateRenderer(Camera2D camera, ContentRegistry contentRegistry = null!)
    {
        var registry = contentRegistry ?? new ContentRegistry();
        var settings = new WorldRenderSettings();
        return new WorldRenderer(camera, new ChunkRenderCacheBuilder(registry, settings), settings);
    }
}
