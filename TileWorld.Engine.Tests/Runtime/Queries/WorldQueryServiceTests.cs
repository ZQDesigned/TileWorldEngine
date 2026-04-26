using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.Operations;
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
    public void OutOfBoundsQuery_ReturnsEmptyCellWithoutCreatingChunk()
    {
        var worldData = new WorldData(new WorldMetadata
        {
            MinTileY = 0,
            MaxTileY = 31
        });
        var registry = CreateRegistry();
        var queryService = new WorldQueryService(worldData, registry);

        var cell = queryService.GetCell(new WorldTileCoord(5, 40), new QueryOptions { LoadChunkIfMissing = true });

        Assert.Equal(TileCell.Empty, cell);
        Assert.False(worldData.HasChunk(new ChunkCoord(0, 1)));
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

    [Fact]
    public void MovementBlocking_ReportsTileObjectAndNone()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.SetForegroundTile(new WorldTileCoord(0, 0), 1);
        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(2, 0),
            100,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });

        Assert.True(objectResult.Success);
        Assert.Equal(MovementBlockerKind.Tile, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(0, 0), EntityType.Player));
        Assert.Equal(MovementBlockerKind.Object, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(2, 0), EntityType.Player));
        Assert.Equal(MovementBlockerKind.None, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(10, 10), EntityType.Player));
    }

    [Fact]
    public void MovementBlocking_UsesCollisionMask()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(0, 0),
            101,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });

        Assert.True(objectResult.Success);
        Assert.Equal(MovementBlockerKind.Object, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(0, 0), EntityType.Player));
        Assert.Equal(MovementBlockerKind.None, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(1, 0), EntityType.Player));
        Assert.Equal(MovementBlockerKind.None, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(0, 1), EntityType.Player));
        Assert.Equal(MovementBlockerKind.Object, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(1, 1), EntityType.Player));
    }

    [Fact]
    public void MovementBlocking_StaysContinuousAcrossChunkBoundary()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(31, 0),
            102,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });

        Assert.True(objectResult.Success);
        Assert.Equal(MovementBlockerKind.Object, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(31, 0), EntityType.Player));
        Assert.Equal(MovementBlockerKind.Object, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(32, 0), EntityType.Player));
    }

    [Fact]
    public void MovementBlocking_RemovingObjectClearsBlocking()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(5, 0),
            100,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });

        Assert.True(objectResult.Success);
        Assert.Equal(MovementBlockerKind.Object, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(5, 0), EntityType.Player));
        Assert.True(runtime.RemoveObject(objectResult.ObjectInstanceId));
        Assert.Equal(MovementBlockerKind.None, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(5, 0), EntityType.Player));
    }

    [Fact]
    public void MovementBlocking_BackgroundWallDoesNotBlock()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();

        Assert.True(runtime.SetBackgroundWall(new WorldTileCoord(8, 8), 1));
        Assert.Equal(MovementBlockerKind.None, runtime.QueryService.GetMovementBlocker(new WorldTileCoord(8, 8), EntityType.Player));
    }

    private static WorldQueryService CreateFixture(out WorldData worldData, out ContentRegistry registry)
    {
        registry = CreateRegistry();

        worldData = new WorldData(new WorldMetadata());
        return new WorldQueryService(worldData, registry);
    }

    private static WorldRuntime CreateRuntime()
    {
        return new WorldRuntime(new WorldData(new WorldMetadata()), CreateRegistry());
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
        registry.RegisterWall(new WallDef
        {
            Id = 1,
            Name = "Stone Wall"
        });
        registry.RegisterObject(new ObjectDef
        {
            Id = 100,
            Name = "Crate",
            SizeInTiles = new TileWorld.Engine.Core.Math.Int2(2, 2),
            RequiresSupport = false,
            MovementCollisionMode = MovementCollisionMode.Solid
        });
        registry.RegisterObject(new ObjectDef
        {
            Id = 101,
            Name = "Mask Object",
            SizeInTiles = new TileWorld.Engine.Core.Math.Int2(2, 2),
            RequiresSupport = false,
            MovementCollisionMode = MovementCollisionMode.Solid,
            CollisionTileMask = [true, false, false, true]
        });
        registry.RegisterObject(new ObjectDef
        {
            Id = 102,
            Name = "Boundary Object",
            SizeInTiles = new TileWorld.Engine.Core.Math.Int2(2, 1),
            RequiresSupport = false,
            MovementCollisionMode = MovementCollisionMode.Solid
        });

        return registry;
    }
}
