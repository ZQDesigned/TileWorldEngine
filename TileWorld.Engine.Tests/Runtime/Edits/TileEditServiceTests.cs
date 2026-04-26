using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Runtime.AutoTile;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Edits;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.Runtime.Tracking;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime.Edits;

public sealed class TileEditServiceTests
{
    [Fact]
    public void SetForegroundTile_CreatesChunkAndWritesTile()
    {
        var fixture = CreateFixture();

        var result = fixture.TileEditService.SetForegroundTile(new WorldTileCoord(0, 0), 1);

        Assert.True(result.Success);
        Assert.True(fixture.WorldData.HasChunk(new ChunkCoord(0, 0)));
        Assert.Equal((ushort)1, fixture.WorldQueryService.GetCell(new WorldTileCoord(0, 0)).ForegroundTileId);
    }

    [Fact]
    public void PlaceTile_ReturnsDirtyFlagsAndPublishesSemanticEvents()
    {
        var fixture = CreateFixture();
        var changedEvents = new List<TileChangedEvent>();
        var placedEvents = new List<TilePlacedEvent>();
        fixture.EventBus.Subscribe<TileChangedEvent>(changedEvents.Add);
        fixture.EventBus.Subscribe<TilePlacedEvent>(placedEvents.Add);

        var result = fixture.TileEditService.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { ActorEntityId = 7, Source = PlacementSource.Player });

        Assert.True(result.Success);
        Assert.Equal(
            ChunkDirtyFlags.RenderDirty | ChunkDirtyFlags.SaveDirty | ChunkDirtyFlags.AutoTileDirty | ChunkDirtyFlags.CollisionDirty | ChunkDirtyFlags.LightDirty | ChunkDirtyFlags.LiquidDirty,
            result.DirtyFlagsApplied);
        Assert.Single(changedEvents);
        Assert.Single(placedEvents);
    }

    [Fact]
    public void PlaceTile_WithInvalidTileIdFails()
    {
        var fixture = CreateFixture();

        var result = fixture.TileEditService.PlaceTile(
            new WorldTileCoord(0, 0),
            0,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        Assert.False(result.Success);
        Assert.Equal(TileEditErrorCode.InvalidTileId, result.ErrorCode);
    }

    [Fact]
    public void PlaceTile_OnOccupiedCellFailsWithoutIgnoreValidation()
    {
        var fixture = CreateFixture();
        fixture.TileEditService.SetForegroundTile(new WorldTileCoord(0, 0), 1);

        var result = fixture.TileEditService.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.Player });

        Assert.False(result.Success);
        Assert.Equal(TileEditErrorCode.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public void BreakTile_OnAirReturnsNoTilePresent()
    {
        var fixture = CreateFixture();

        var result = fixture.TileEditService.BreakTile(
            new WorldTileCoord(10, 10),
            new TileBreakContext { Source = BreakSource.Player });

        Assert.False(result.Success);
        Assert.Equal(TileEditErrorCode.NoTilePresent, result.ErrorCode);
        Assert.False(fixture.WorldData.HasChunk(new ChunkCoord(0, 0)));
    }

    [Fact]
    public void BreakTile_OnNonMineableTileReturnsTileNotMineable()
    {
        var fixture = CreateFixture();
        fixture.TileEditService.SetForegroundTile(new WorldTileCoord(0, 0), 2);

        var result = fixture.TileEditService.BreakTile(
            new WorldTileCoord(0, 0),
            new TileBreakContext { Source = BreakSource.Player });

        Assert.False(result.Success);
        Assert.Equal(TileEditErrorCode.TileNotMineable, result.ErrorCode);
    }

    [Fact]
    public void RemoveForegroundTile_DoesNotCreateChunkForMissingTile()
    {
        var fixture = CreateFixture();

        var result = fixture.TileEditService.RemoveForegroundTile(new WorldTileCoord(100, 100));

        Assert.False(result.Success);
        Assert.Equal(TileEditErrorCode.NoTilePresent, result.ErrorCode);
        Assert.Equal(0, fixture.WorldData.LoadedChunkCount);
    }

    [Fact]
    public void SuppressEvents_PreventsPublishing()
    {
        var fixture = CreateFixture();
        var changedEvents = new List<TileChangedEvent>();
        var placedEvents = new List<TilePlacedEvent>();
        fixture.EventBus.Subscribe<TileChangedEvent>(changedEvents.Add);
        fixture.EventBus.Subscribe<TilePlacedEvent>(placedEvents.Add);

        var result = fixture.TileEditService.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext
            {
                Source = PlacementSource.DebugTool,
                SuppressEvents = true
            });

        Assert.True(result.Success);
        Assert.Empty(changedEvents);
        Assert.Empty(placedEvents);
    }

    [Fact]
    public void DirectModification_OnlyPublishesTileChangedEvent()
    {
        var fixture = CreateFixture();
        var changedEvents = new List<TileChangedEvent>();
        var placedEvents = new List<TilePlacedEvent>();
        fixture.EventBus.Subscribe<TileChangedEvent>(changedEvents.Add);
        fixture.EventBus.Subscribe<TilePlacedEvent>(placedEvents.Add);

        var result = fixture.TileEditService.SetForegroundTile(new WorldTileCoord(0, 0), 1);

        Assert.True(result.Success);
        Assert.Single(changedEvents);
        Assert.Empty(placedEvents);
    }

    [Fact]
    public void BoundaryModification_MarksNeighborChunkDirty()
    {
        var fixture = CreateFixture();
        fixture.WorldData.GetOrCreateChunk(new ChunkCoord(1, 0));

        var result = fixture.TileEditService.PlaceTile(
            new WorldTileCoord(31, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        Assert.True(result.Success);
        Assert.True(fixture.DirtyTracker.HasDirty(new ChunkCoord(1, 0), ChunkDirtyFlags.RenderDirty));
        Assert.True(fixture.DirtyTracker.HasDirty(new ChunkCoord(1, 0), ChunkDirtyFlags.AutoTileDirty));
        Assert.True(fixture.DirtyTracker.HasDirty(new ChunkCoord(1, 0), ChunkDirtyFlags.CollisionDirty));
    }

    private static Fixture CreateFixture()
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
            Name = "Bedrock",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = false,
            Hardness = 999,
            AutoTileGroupId = 1
        });

        var worldData = new WorldData(new WorldMetadata());
        var queryService = new WorldQueryService(worldData, registry);
        var dirtyTracker = new DirtyTracker(worldData);
        var eventBus = new WorldEventBus();
        var autoTileSystem = new AutoTileSystem(worldData, queryService);
        var tileEditService = new TileEditService(worldData, registry, queryService, dirtyTracker, eventBus, autoTileSystem);

        return new Fixture(worldData, queryService, dirtyTracker, eventBus, tileEditService);
    }

    private sealed record Fixture(
        WorldData WorldData,
        WorldQueryService WorldQueryService,
        DirtyTracker DirtyTracker,
        WorldEventBus EventBus,
        TileEditService TileEditService);
}
