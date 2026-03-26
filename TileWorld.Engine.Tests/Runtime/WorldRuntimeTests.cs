using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.World;
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
            AutoTileGroupId = 1
        });

        return new WorldRuntime(new WorldData(new WorldMetadata()), registry);
    }
}
