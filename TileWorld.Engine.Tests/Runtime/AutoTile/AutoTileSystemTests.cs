using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Runtime.AutoTile;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime.AutoTile;

public sealed class AutoTileSystemTests
{
    [Fact]
    public void ComputeVariant_ForIsolatedTile_IsZero()
    {
        var (worldData, _, autoTileSystem) = CreateAutoTileFixture();
        var chunk = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        chunk.SetCell(0, 0, new TileCell { ForegroundTileId = 1 });

        Assert.Equal((ushort)0, autoTileSystem.ComputeVariant(new WorldTileCoord(0, 0)));
    }

    [Fact]
    public void RefreshAround_AssignsStableFourNeighborMask()
    {
        var (worldData, _, autoTileSystem) = CreateAutoTileFixture();
        var chunk = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));

        chunk.SetCell(0, 0, new TileCell { ForegroundTileId = 1 });
        chunk.SetCell(1, 0, new TileCell { ForegroundTileId = 1 });

        autoTileSystem.RefreshAround(new WorldTileCoord(0, 0));

        Assert.Equal((ushort)2, chunk.GetCell(0, 0).Variant);
        Assert.Equal((ushort)8, chunk.GetCell(1, 0).Variant);
    }

    [Fact]
    public void RefreshAround_CanConnectAcrossChunkBoundaries()
    {
        var (worldData, _, autoTileSystem) = CreateAutoTileFixture();
        var leftChunk = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        var rightChunk = worldData.GetOrCreateChunk(new ChunkCoord(1, 0));

        leftChunk.SetCell(31, 0, new TileCell { ForegroundTileId = 1 });
        rightChunk.SetCell(0, 0, new TileCell { ForegroundTileId = 1 });

        autoTileSystem.RefreshAround(new WorldTileCoord(31, 0));

        Assert.Equal((ushort)2, leftChunk.GetCell(31, 0).Variant);
        Assert.Equal((ushort)8, rightChunk.GetCell(0, 0).Variant);
    }

    [Fact]
    public void ComputeVariant_ForAirOrNonConnectedTile_IsZero()
    {
        var (worldData, _, autoTileSystem) = CreateAutoTileFixture();
        var chunk = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        chunk.SetCell(0, 0, new TileCell { ForegroundTileId = 2 });

        Assert.Equal((ushort)0, autoTileSystem.ComputeVariant(new WorldTileCoord(0, 0)));
        Assert.Equal((ushort)0, autoTileSystem.ComputeVariant(new WorldTileCoord(5, 5)));
    }

    private static (WorldData WorldData, WorldQueryService QueryService, AutoTileSystem AutoTileSystem) CreateAutoTileFixture()
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
        registry.RegisterTile(new TileDef
        {
            Id = 2,
            Name = "Wood",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = false,
            CanBeMined = true,
            Hardness = 1,
            AutoTileGroupId = 0
        });

        var worldData = new WorldData(new WorldMetadata());
        var queryService = new WorldQueryService(worldData, registry);
        var autoTileSystem = new AutoTileSystem(worldData, queryService);
        return (worldData, queryService, autoTileSystem);
    }
}
