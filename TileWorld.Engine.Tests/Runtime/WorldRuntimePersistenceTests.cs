using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Render;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.Storage;
using TileWorld.Engine.Tests.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime;

public sealed class WorldRuntimePersistenceTests
{
    [Fact]
    public void SaveWorld_PersistsOnlySaveDirtyChunksAndClearsFlag()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateRuntime(directory.Path);

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        runtime.SaveWorld();

        Assert.False(runtime.DirtyTracker.HasDirty(new ChunkCoord(0, 0), ChunkDirtyFlags.SaveDirty));
        Assert.True(File.Exists(Path.Combine(directory.Path, "world.json")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "chunks", "0_0.chk")));
    }

    [Fact]
    public void Shutdown_WhenPersistenceIsEnabled_SavesWorld()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateRuntime(directory.Path);

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(2, 2),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        runtime.Shutdown();

        var storage = new WorldStorage();
        var restoredChunk = storage.TryLoadChunk(directory.Path, new ChunkCoord(0, 0));

        Assert.NotNull(restoredChunk);
        Assert.Equal((ushort)1, restoredChunk.GetCell(2, 2).ForegroundTileId);
    }

    [Fact]
    public void Shutdown_WhenPersistenceIsDisabled_DoesNotWriteFiles()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateRuntime(string.Empty);

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(1, 1),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        runtime.Shutdown();

        Assert.False(File.Exists(Path.Combine(directory.Path, "world.json")));
    }

    [Fact]
    public void RestartRuntime_RestoresSavedTileVariantWallAndLiquidData()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateRuntime(directory.Path);

        runtime.Initialize();
        var chunk = runtime.ChunkManager.GetOrLoadChunk(new ChunkCoord(0, 0));
        chunk.SetCell(3, 4, new TileCell
        {
            ForegroundTileId = 1,
            BackgroundWallId = 9,
            LiquidType = 2,
            LiquidAmount = 111,
            Variant = 6,
            Flags = 13
        });
        runtime.DirtyTracker.MarkDirty(chunk.Coord, ChunkDirtyFlags.SaveDirty | ChunkDirtyFlags.RenderDirty);
        runtime.SaveWorld();
        runtime.Shutdown();

        var metadata = new WorldStorage().LoadMetadata(directory.Path);
        var restoredRuntime = CreateRuntime(directory.Path, metadata);
        restoredRuntime.Initialize();

        var restoredCell = restoredRuntime.QueryService.GetCell(
            new WorldTileCoord(3, 4),
            new QueryOptions { LoadChunkIfMissing = true });

        Assert.Equal((ushort)1, restoredCell.ForegroundTileId);
        Assert.Equal((ushort)9, restoredCell.BackgroundWallId);
        Assert.Equal((byte)2, restoredCell.LiquidType);
        Assert.Equal((byte)111, restoredCell.LiquidAmount);
        Assert.Equal((ushort)6, restoredCell.Variant);
        Assert.Equal((ushort)13, restoredCell.Flags);
    }

    [Fact]
    public void Update_WhenIdleAutoSaveDelayElapses_PersistsDirtyChunks()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateRuntime(
            directory.Path,
            options: new WorldRuntimeOptions
            {
                WorldPath = directory.Path,
                WorldStorage = new WorldStorage(),
                SaveOnShutdown = true,
                EnableAutoSave = true,
                AutoSaveInterval = TimeSpan.FromSeconds(30),
                AutoSaveIdleDelay = TimeSpan.FromSeconds(2),
                MinimumAutoSaveSpacing = TimeSpan.FromSeconds(1)
            });

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(4, 4),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        runtime.Update(new FrameTime(TimeSpan.Zero, TimeSpan.Zero, false));
        Assert.False(File.Exists(Path.Combine(directory.Path, "chunks", "0_0.chk")));

        runtime.Update(new FrameTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), false));

        Assert.True(File.Exists(Path.Combine(directory.Path, "chunks", "0_0.chk")));
        Assert.False(runtime.DirtyTracker.HasDirty(new ChunkCoord(0, 0), ChunkDirtyFlags.SaveDirty));
    }

    [Fact]
    public void Update_WhenPeriodicAutoSaveIntervalElapses_PersistsDirtyChunks()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateRuntime(
            directory.Path,
            options: new WorldRuntimeOptions
            {
                WorldPath = directory.Path,
                WorldStorage = new WorldStorage(),
                SaveOnShutdown = true,
                EnableAutoSave = true,
                AutoSaveInterval = TimeSpan.FromSeconds(5),
                AutoSaveIdleDelay = TimeSpan.FromSeconds(20),
                MinimumAutoSaveSpacing = TimeSpan.FromSeconds(1)
            });

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(7, 3),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        runtime.Update(new FrameTime(TimeSpan.Zero, TimeSpan.Zero, false));
        runtime.Update(new FrameTime(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), false));

        var restoredChunk = new WorldStorage().TryLoadChunk(directory.Path, new ChunkCoord(0, 0));
        Assert.NotNull(restoredChunk);
        Assert.Equal((ushort)1, restoredChunk.GetCell(7, 3).ForegroundTileId);
    }

    private static WorldRuntime CreateRuntime(string worldPath, WorldMetadata metadata = null!, WorldRuntimeOptions options = null!)
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
            Visual = new TileVisualDef("debug/white", new RectI(0, 0, 1, 1), new ColorRgba32(128, 128, 128), false)
        });

        if (string.IsNullOrWhiteSpace(worldPath))
        {
            return new WorldRuntime(new WorldData(metadata ?? new WorldMetadata()), registry);
        }

        return new WorldRuntime(
            new WorldData(metadata ?? new WorldMetadata { WorldId = "runtime-world", Name = "Runtime World" }),
            registry,
            options ?? new WorldRuntimeOptions
            {
                WorldPath = worldPath,
                WorldStorage = new WorldStorage(),
                SaveOnShutdown = true
            });
    }
}
