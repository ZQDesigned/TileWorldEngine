using TileWorld.Engine.Content.Items;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.Tests.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime;

public sealed class WorldRuntimeGameplayIntegrationTests
{
    [Fact]
    public void BackgroundWalls_CanBePlacedAndRemoved()
    {
        var runtime = CreateRuntime();
        var dropEvents = new List<DropSpawnedEvent>();
        runtime.Subscribe<DropSpawnedEvent>(dropEvents.Add);

        runtime.Initialize();

        Assert.True(runtime.SetBackgroundWall(new WorldTileCoord(2, 3), 1));
        Assert.True(runtime.HasBackgroundWall(new WorldTileCoord(2, 3)));
        Assert.True(runtime.RemoveBackgroundWall(new WorldTileCoord(2, 3)));
        Assert.False(runtime.HasBackgroundWall(new WorldTileCoord(2, 3)));
        var dropEvent = Assert.Single(dropEvents);
        Assert.Equal(1101, dropEvent.ItemDefId);
    }

    [Fact]
    public void ObjectPlacement_BlocksTilePlacementOnOccupiedCells()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        SeedSupportFloor(runtime, 0, 1, 2);

        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(0, 0),
            100,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });
        var tileResult = runtime.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        Assert.True(objectResult.Success);
        Assert.False(tileResult.Success);
    }

    [Fact]
    public void BreakingSupport_RemovesSupportedObject()
    {
        var runtime = CreateRuntime();
        var removedEvents = new List<ObjectRemovedEvent>();
        runtime.Subscribe<ObjectRemovedEvent>(removedEvents.Add);
        runtime.Initialize();
        SeedSupportFloor(runtime, 0, 1, 2);
        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(0, 0),
            100,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });

        var breakResult = runtime.BreakTile(
            new WorldTileCoord(0, 2),
            new TileBreakContext
            {
                Source = BreakSource.DebugTool,
                SpawnDrops = true
            });

        Assert.True(objectResult.Success);
        Assert.True(breakResult.Success);
        Assert.Single(removedEvents);
        Assert.False(runtime.TryGetObject(objectResult.ObjectInstanceId, out _));
    }

    [Fact]
    public void SaveAndReload_PreservesWallsAndObjects()
    {
        using var directory = new TestDirectoryScope();
        var worldPath = directory.Path;
        var registry = CreateRegistry();
        var runtime = new WorldRuntime(
            new WorldData(new WorldMetadata { WorldId = "persistence", Name = "Persistence Test" }),
            registry,
            new WorldRuntimeOptions
            {
                WorldPath = worldPath,
                WorldStorage = new TileWorld.Engine.Storage.WorldStorage()
            });

        runtime.Initialize();
        SeedSupportFloor(runtime, 0, 1, 2);
        runtime.SetBackgroundWall(new WorldTileCoord(1, 0), 1);
        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(0, 0),
            100,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });
        runtime.SaveWorld();
        runtime.Shutdown();

        var restoredRuntime = new WorldRuntime(
            new WorldData(new TileWorld.Engine.Storage.WorldStorage().LoadMetadata(worldPath)),
            CreateRegistry(),
            new WorldRuntimeOptions
            {
                WorldPath = worldPath,
                WorldStorage = new TileWorld.Engine.Storage.WorldStorage()
            });

        restoredRuntime.Initialize();
        restoredRuntime.LoadChunk(new TileWorld.Engine.World.Coordinates.ChunkCoord(0, 0));

        Assert.True(objectResult.Success);
        Assert.True(restoredRuntime.HasBackgroundWall(new WorldTileCoord(1, 0)));
        Assert.True(restoredRuntime.TryGetObject(objectResult.ObjectInstanceId, out var restoredObject));
        Assert.Equal(new WorldTileCoord(0, 0), restoredObject.AnchorCoord);
    }

    [Fact]
    public void ActiveChunkSet_LoadsAndUnloadsAroundCenter()
    {
        using var directory = new TestDirectoryScope();
        var runtime = new WorldRuntime(
            new WorldData(new WorldMetadata()),
            CreateRegistry(),
            new WorldRuntimeOptions
            {
                WorldPath = directory.Path,
                WorldStorage = new TileWorld.Engine.Storage.WorldStorage(),
                ActiveRadiusInChunks = 1
            });

        runtime.Initialize();
        runtime.EnsureActiveAround(new WorldTileCoord(0, 0));
        var firstActive = runtime.GetActiveChunks().ToArray();
        runtime.EnsureActiveAround(new WorldTileCoord(96, 0));
        var secondActive = runtime.GetActiveChunks().ToArray();

        Assert.Equal(9, firstActive.Length);
        Assert.Contains(new TileWorld.Engine.World.Coordinates.ChunkCoord(0, 0), firstActive);
        Assert.Contains(new TileWorld.Engine.World.Coordinates.ChunkCoord(3, 0), secondActive);
        Assert.DoesNotContain(new TileWorld.Engine.World.Coordinates.ChunkCoord(0, 0), secondActive);
    }

    [Fact]
    public void PlayerPhysics_StopsOnGroundAndCollectsDrops()
    {
        var runtime = CreateRuntime();
        var collectedEvents = new List<DropCollectedEvent>();
        runtime.Subscribe<DropCollectedEvent>(collectedEvents.Add);
        runtime.Initialize();
        SeedSupportFloor(runtime, -4, 12, 4);
        var playerId = runtime.SpawnPlayer(new Float2(0.1f, 0.5f));
        runtime.SetForegroundTile(new WorldTileCoord(1, 3), 1);

        runtime.BreakTile(
            new WorldTileCoord(1, 3),
            new TileBreakContext
            {
                Source = BreakSource.DebugTool,
                SpawnDrops = true
            });

        runtime.SetPlayerInput(playerId, 1f, jumpRequested: false);
        for (var frame = 0; frame < 24; frame++)
        {
            runtime.Update(new FrameTime(TimeSpan.FromSeconds(frame / 60d), TimeSpan.FromSeconds(1d / 60d), false));
        }

        Assert.True(runtime.TryGetEntity(playerId, out var player));
        Assert.True(player.Position.Y < 4f);
        Assert.NotEmpty(collectedEvents);
    }

    [Fact]
    public void PlayerPhysics_SolidObjectBlocksHorizontalTraversal()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        SeedSupportFloor(runtime, -2, 8, 2);
        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(2, 0),
            100,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });
        var playerId = runtime.SpawnPlayer(new Float2(0.1f, 0.05f));

        runtime.SetPlayerInput(playerId, 1f, jumpRequested: false);
        for (var frame = 0; frame < 120; frame++)
        {
            runtime.Update(new FrameTime(TimeSpan.FromSeconds(frame / 60d), TimeSpan.FromSeconds(1d / 60d), false));
        }

        Assert.True(objectResult.Success);
        Assert.True(runtime.TryGetEntity(playerId, out var player));
        Assert.True(player.Position.X < 1.2f, $"Expected player to be blocked by object, actual X={player.Position.X:0.000}.");
    }

    [Fact]
    public void PlayerPhysics_NonBlockingObjectAllowsHorizontalTraversal()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        SeedSupportFloor(runtime, -2, 8, 2);
        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(2, 0),
            103,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });
        var playerId = runtime.SpawnPlayer(new Float2(0.1f, 0.05f));

        runtime.SetPlayerInput(playerId, 1f, jumpRequested: false);
        for (var frame = 0; frame < 120; frame++)
        {
            runtime.Update(new FrameTime(TimeSpan.FromSeconds(frame / 60d), TimeSpan.FromSeconds(1d / 60d), false));
        }

        Assert.True(objectResult.Success);
        Assert.True(runtime.TryGetEntity(playerId, out var player));
        Assert.True(player.Position.X > 2.2f, $"Expected player to pass through non-blocking object, actual X={player.Position.X:0.000}.");
    }

    [Fact]
    public void SpawnedDrop_UsesCenteredBoundsWithinSourceTile()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();

        var dropId = runtime.EntityManager.SpawnDrop(1001, new Float2(4.5f, 6.5f));

        Assert.True(runtime.TryGetEntity(dropId, out var drop));
        Assert.InRange(drop.WorldBounds.Left, 4f, 4.5f);
        Assert.InRange(drop.WorldBounds.Right, 4.5f, 5f);
        Assert.InRange(drop.WorldBounds.Top, 6f, 6.5f);
        Assert.InRange(drop.WorldBounds.Bottom, 6.5f, 7f);
    }

    [Fact]
    public void ObjectPlacement_FailsWhenFootprintLeavesVerticalBounds()
    {
        var runtime = new WorldRuntime(
            new WorldData(new WorldMetadata
            {
                MinTileY = 0,
                MaxTileY = 3
            }),
            CreateRegistry());
        runtime.Initialize();
        SeedSupportFloor(runtime, 0, 1, 2);

        var objectResult = runtime.PlaceObject(
            new WorldTileCoord(0, 3),
            100,
            new ObjectPlacementContext { Source = PlacementSource.DebugTool });

        Assert.False(objectResult.Success);
        Assert.Equal(TileWorld.Engine.Runtime.Objects.ObjectPlacementErrorCode.OutOfBounds, objectResult.ErrorCode);
    }

    [Fact]
    public void PlayerPhysics_ClampsToConfiguredVerticalBounds()
    {
        var runtime = new WorldRuntime(
            new WorldData(new WorldMetadata
            {
                MinTileY = 0,
                MaxTileY = 5
            }),
            CreateRegistry());
        runtime.Initialize();
        var playerId = runtime.SpawnPlayer(new Float2(0.5f, 0.25f));

        for (var frame = 0; frame < 90; frame++)
        {
            runtime.Update(new FrameTime(TimeSpan.FromSeconds(frame / 60d), TimeSpan.FromSeconds(1d / 60d), false));
        }

        Assert.True(runtime.TryGetEntity(playerId, out var player));
        Assert.True(player.Position.Y + player.LocalBounds.Bottom <= 6f);
    }

    private static void SeedSupportFloor(WorldRuntime runtime, int minX, int maxX, int y)
    {
        for (var x = minX; x <= maxX; x++)
        {
            runtime.SetForegroundTile(new WorldTileCoord(x, y), 1);
        }
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
            BreakDropItemId = 1001,
            AutoTileGroupId = 1
        });
        registry.RegisterWall(new WallDef
        {
            Id = 1,
            Name = "Stone Wall",
            CountsAsRoomWall = true,
            BreakDropItemId = 1101
        });
        registry.RegisterItem(new ItemDef
        {
            Id = 1001,
            Name = "Stone Block"
        });
        registry.RegisterItem(new ItemDef
        {
            Id = 1101,
            Name = "Stone Wall",
            PlaceWallId = 1
        });
        registry.RegisterItem(new ItemDef
        {
            Id = 2001,
            Name = "Crate"
        });
        registry.RegisterObject(new ObjectDef
        {
            Id = 100,
            Name = "Crate",
            SizeInTiles = new Int2(2, 2),
            RequiresSupport = true,
            BreakDropItemId = 2001,
            MovementCollisionMode = MovementCollisionMode.Solid
        });
        registry.RegisterObject(new ObjectDef
        {
            Id = 103,
            Name = "Banner",
            SizeInTiles = new Int2(2, 2),
            RequiresSupport = true,
            MovementCollisionMode = MovementCollisionMode.None
        });

        return registry;
    }
}
