using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime;

public sealed class WorldRuntimeTests
{
    [Fact]
    public void UnifiedEntryPoints_DelegateToUnderlyingServices()
    {
        var runtime = CreateRuntime();

        runtime.Initialize();

        var placeResult = runtime.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        runtime.Update(new FrameTime(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16), false));

        Assert.True(placeResult.Success);
        Assert.True(runtime.IsSolid(new WorldTileCoord(0, 0)));
        Assert.Equal((ushort)1, runtime.GetCell(new WorldTileCoord(0, 0)).ForegroundTileId);

        var removeResult = runtime.RemoveForegroundTile(new WorldTileCoord(0, 0));

        Assert.True(removeResult.Success);
        Assert.False(runtime.IsSolid(new WorldTileCoord(0, 0)));
    }

    [Fact]
    public void LifecycleMethods_AreSafeToCallMultipleTimes()
    {
        var runtime = CreateRuntime();

        runtime.Initialize();
        runtime.Initialize();
        runtime.Update(default);
        runtime.Shutdown();
        runtime.Shutdown();
    }

    [Fact]
    public void OutOfBoundsWorlds_RejectTilePlacement()
    {
        var runtime = CreateRuntime(new WorldMetadata
        {
            MinTileY = 0,
            MaxTileY = 31
        });

        runtime.Initialize();
        var outOfBoundsCoord = new WorldTileCoord(0, 40);
        var placeResult = runtime.PlaceTile(
            outOfBoundsCoord,
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        Assert.False(placeResult.Success);
        Assert.Equal(TileWorld.Engine.Runtime.Edits.TileEditErrorCode.OutOfBounds, placeResult.ErrorCode);
        Assert.False(runtime.IsWithinWorldBounds(outOfBoundsCoord));
    }

    [Fact]
    public void EnsureActiveForTileArea_LoadsChunksCoveringTheRequestedBounds()
    {
        using var directory = new TileWorld.Engine.Tests.Storage.TestDirectoryScope();
        var runtime = CreatePersistedRuntime(directory.Path);

        runtime.Initialize();
        runtime.EnsureActiveForTileArea(new RectI(0, 0, 64, 32));

        Assert.Contains(new ChunkCoord(0, 0), runtime.GetActiveChunks());
        Assert.Contains(new ChunkCoord(1, 0), runtime.GetActiveChunks());
    }

    private static WorldRuntime CreateRuntime(WorldMetadata? metadata = null)
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
            AutoTileGroupId = 1
        });

        return new WorldRuntime(new WorldData(metadata ?? new WorldMetadata()), registry);
    }

    private static WorldRuntime CreatePersistedRuntime(string worldPath)
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
            AutoTileGroupId = 1
        });

        return new WorldRuntime(
            new WorldData(new WorldMetadata
            {
                WorldId = "runtime-active-window",
                Name = "Runtime Active Window",
                Seed = 1234
            }),
            registry,
            new WorldRuntimeOptions
            {
                WorldPath = worldPath,
                WorldStorage = new TileWorld.Engine.Storage.WorldStorage(),
                SaveOnShutdown = false,
                EnableAutoSave = false
            });
    }
}
