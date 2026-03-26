using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.Storage;
using TileWorld.Engine.Tests.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime.Queries;

public sealed class WorldQueryServiceTests
{
    [Fact]
    public void MissingChunk_DefaultsToEmptyCell()
    {
        var queryService = CreateFixture(out _, out _);

        var cell = queryService.GetCell(new WorldTileCoord(5, 5));

        Assert.Equal(TileCell.Empty, cell);
    }

    [Fact]
    public void LoadChunkIfMissing_CreatesChunk()
    {
        var queryService = CreateFixture(out var worldData, out _);

        var found = queryService.TryGetCell(
            new WorldTileCoord(5, 5),
            out _,
            new QueryOptions { LoadChunkIfMissing = true, ReturnDefaultWhenMissing = false });

        Assert.True(found);
        Assert.True(worldData.HasChunk(new ChunkCoord(0, 0)));
    }

    [Fact]
    public void LoadChunkIfMissing_UsesChunkManagerToRestoreStoredChunk()
    {
        using var directory = new TestDirectoryScope();
        var storage = new WorldStorage();
        storage.SaveMetadata(directory.Path, new WorldMetadata());

        var storedChunk = new Chunk(new ChunkCoord(0, 0));
        storedChunk.SetCell(5, 5, new TileCell { ForegroundTileId = 1, Variant = 2 });
        storage.SaveChunk(directory.Path, storedChunk);

        var worldData = new WorldData(new WorldMetadata());
        var registry = CreateRegistry();
        var queryService = new WorldQueryService(worldData, registry, new ChunkManager(worldData, storage, directory.Path));

        var cell = queryService.GetCell(
            new WorldTileCoord(5, 5),
            new QueryOptions { LoadChunkIfMissing = true, ReturnDefaultWhenMissing = false });

        Assert.Equal((ushort)1, cell.ForegroundTileId);
        Assert.Equal((ushort)2, cell.Variant);
        Assert.True(worldData.HasChunk(new ChunkCoord(0, 0)));
    }

    [Fact]
    public void SemanticQueries_UseTileDefinitions()
    {
        var queryService = CreateFixture(out var worldData, out _);
        var chunk = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        chunk.SetCell(0, 0, new TileCell { ForegroundTileId = 1 });

        Assert.True(queryService.IsSolid(new WorldTileCoord(0, 0)));
        Assert.False(queryService.IsEmpty(new WorldTileCoord(0, 0)));
        Assert.True(queryService.BlocksLight(new WorldTileCoord(0, 0)));
    }

    [Fact]
    public void NeighborEnumeration_UsesStableOrdering()
    {
        var queryService = CreateFixture(out _, out _);

        Assert.Equal(
            [
                new WorldTileCoord(10, 9),
                new WorldTileCoord(11, 10),
                new WorldTileCoord(10, 11),
                new WorldTileCoord(9, 10)
            ],
            queryService.EnumerateNeighbors4(new WorldTileCoord(10, 10)).ToArray());

        Assert.Equal(8, queryService.EnumerateNeighbors8(new WorldTileCoord(10, 10)).Count());
    }

    private static WorldQueryService CreateFixture(out WorldData worldData, out ContentRegistry registry)
    {
        registry = CreateRegistry();

        worldData = new WorldData(new WorldMetadata());
        return new WorldQueryService(worldData, registry);
    }

    private static ContentRegistry CreateRegistry()
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

        return registry;
    }
}
