using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.AutoTile;
using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Lighting;
using TileWorld.Engine.Runtime.Objects;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.Runtime.Support;
using TileWorld.Engine.Runtime.Tracking;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Edits;

/// <summary>
/// Provides the single write path for modifying tile foreground data.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="Runtime.WorldRuntime"/> so edit
/// behavior can continue evolving without exposing lower-level orchestration details.
/// </remarks>
internal sealed class TileEditService
{
    private readonly AutoTileSystem _autoTileSystem;
    private readonly ContentRegistry _contentRegistry;
    private readonly ChunkManager _chunkManager;
    private readonly DirtyTracker _dirtyTracker;
    private readonly EntityManager _entityManager;
    private readonly WorldEventBus _eventBus;
    private readonly LightingSystem _lightingSystem;
    private readonly ObjectManager _objectManager;
    private readonly SupportSystem _supportSystem;
    private readonly WorldQueryService _worldQueryService;
    private readonly WorldData _worldData;

    /// <summary>
    /// Creates a tile editing service over the supplied world services.
    /// </summary>
    /// <param name="worldData">The world data that will be mutated.</param>
    /// <param name="contentRegistry">The registry used to validate and inspect tile definitions.</param>
    /// <param name="worldQueryService">The query service used to inspect existing world state.</param>
    /// <param name="dirtyTracker">The dirty tracker used to propagate chunk dirty flags.</param>
    /// <param name="eventBus">The event bus used to publish tile-change notifications.</param>
    /// <param name="autoTileSystem">The autotile system used to refresh tile variants after edits.</param>
    /// <param name="objectManager">The optional object manager used to validate object occupancy.</param>
    /// <param name="supportSystem">The optional support system used to react to support loss.</param>
    /// <param name="entityManager">The optional entity manager used to spawn drops.</param>
    /// <param name="chunkManager">The optional chunk manager used to load missing chunks before writes.</param>
    /// <param name="lightingSystem">The optional lighting system used to invalidate derived light buffers after edits.</param>
    public TileEditService(
        WorldData worldData,
        ContentRegistry contentRegistry,
        WorldQueryService worldQueryService,
        DirtyTracker dirtyTracker,
        WorldEventBus eventBus,
        AutoTileSystem autoTileSystem,
        ObjectManager objectManager = null,
        SupportSystem supportSystem = null,
        EntityManager entityManager = null,
        ChunkManager chunkManager = null,
        LightingSystem lightingSystem = null)
    {
        _worldData = worldData;
        _contentRegistry = contentRegistry;
        _worldQueryService = worldQueryService;
        _dirtyTracker = dirtyTracker;
        _eventBus = eventBus;
        _autoTileSystem = autoTileSystem;
        _lightingSystem = lightingSystem;
        _objectManager = objectManager;
        _supportSystem = supportSystem;
        _entityManager = entityManager;
        _chunkManager = chunkManager;
    }

    /// <summary>
    /// Writes a non-air foreground tile directly to the world.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="tileId">The non-air tile identifier to write.</param>
    /// <returns>The outcome of the write operation.</returns>
    public TileEditResult SetForegroundTile(WorldTileCoord coord, ushort tileId)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return TileEditResult.Failed(TileEditErrorCode.OutOfBounds, coord);
        }

        if (tileId == 0)
        {
            return TileEditResult.Failed(TileEditErrorCode.InvalidTileId, coord);
        }

        if (!_contentRegistry.HasTileDef(tileId))
        {
            return TileEditResult.Failed(TileEditErrorCode.InvalidTileId, coord);
        }

        return SetForegroundTileCore(
            coord,
            tileId,
            publishSemanticEvent: false,
            semanticPlacementContext: null,
            semanticBreakContext: null,
            suppressEvents: false);
    }

    /// <summary>
    /// Removes the foreground tile at the supplied coordinate.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <returns>The outcome of the removal operation.</returns>
    public TileEditResult RemoveForegroundTile(WorldTileCoord coord)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return TileEditResult.Failed(TileEditErrorCode.OutOfBounds, coord);
        }

        if (!TryGetExistingTile(coord, out var previousTileId))
        {
            return TileEditResult.Failed(TileEditErrorCode.NoTilePresent, coord);
        }

        return SetForegroundTileCore(
            coord,
            0,
            publishSemanticEvent: false,
            semanticPlacementContext: null,
            semanticBreakContext: null,
            suppressEvents: false);
    }

    /// <summary>
    /// Writes a non-empty background wall directly to the world.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="wallId">The non-empty wall identifier to write.</param>
    /// <returns><see langword="true"/> when the write succeeds.</returns>
    public bool SetBackgroundWall(WorldTileCoord coord, ushort wallId)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return false;
        }

        if (wallId == 0 || !_contentRegistry.HasWallDef(wallId))
        {
            return false;
        }

        return SetBackgroundWallCore(coord, wallId);
    }

    /// <summary>
    /// Removes the background wall at the supplied coordinate.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <returns><see langword="true"/> when a background wall existed and was removed.</returns>
    public bool RemoveBackgroundWall(WorldTileCoord coord)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return false;
        }

        var currentCell = _worldQueryService.GetCell(coord);
        return currentCell.BackgroundWallId != 0 && SetBackgroundWallCore(coord, 0);
    }

    /// <summary>
    /// Places a tile using placement semantics and validation rules.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="tileId">The non-air tile identifier to place.</param>
    /// <param name="context">Placement metadata and behavior flags.</param>
    /// <returns>The outcome of the placement attempt.</returns>
    public TileEditResult PlaceTile(WorldTileCoord coord, ushort tileId, TilePlacementContext context)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return TileEditResult.Failed(TileEditErrorCode.OutOfBounds, coord);
        }

        if (tileId == 0 || !_contentRegistry.TryGetTileDef(tileId, out _))
        {
            return TileEditResult.Failed(TileEditErrorCode.InvalidTileId, coord);
        }

        EnsureChunkLoadedForRead(coord);

        if (!context.IgnoreValidation && !CanPlaceTile(coord, tileId, context))
        {
            return TileEditResult.Failed(TileEditErrorCode.ValidationFailed, coord, GetCurrentTileId(coord));
        }

        return SetForegroundTileCore(
            coord,
            tileId,
            publishSemanticEvent: true,
            semanticPlacementContext: context,
            semanticBreakContext: null,
            suppressEvents: context.SuppressEvents);
    }

    /// <summary>
    /// Breaks a tile using break semantics and validation rules.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="context">Break metadata and behavior flags.</param>
    /// <returns>The outcome of the break attempt.</returns>
    public TileEditResult BreakTile(WorldTileCoord coord, TileBreakContext context)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return TileEditResult.Failed(TileEditErrorCode.OutOfBounds, coord);
        }

        EnsureChunkLoadedForRead(coord);

        if (!TryGetExistingTile(coord, out var previousTileId))
        {
            return TileEditResult.Failed(TileEditErrorCode.NoTilePresent, coord);
        }

        if (!context.IgnoreHardness &&
            _contentRegistry.TryGetTileDef(previousTileId, out var tileDef) &&
            !tileDef.CanBeMined)
        {
            return TileEditResult.Failed(TileEditErrorCode.TileNotMineable, coord, previousTileId);
        }

        return SetForegroundTileCore(
            coord,
            0,
            publishSemanticEvent: true,
            semanticPlacementContext: null,
            semanticBreakContext: context,
            suppressEvents: context.SuppressEvents);
    }

    /// <summary>
    /// Evaluates whether a tile placement would currently succeed.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="tileId">The non-air tile identifier to test.</param>
    /// <param name="context">Placement metadata and behavior flags.</param>
    /// <returns><see langword="true"/> when the placement would currently succeed.</returns>
    public bool CanPlaceTile(WorldTileCoord coord, ushort tileId, TilePlacementContext context)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return false;
        }

        EnsureChunkLoadedForRead(coord);

        if (tileId == 0 || !_contentRegistry.HasTileDef(tileId))
        {
            return false;
        }

        if (!context.IgnoreValidation)
        {
            if (!_worldQueryService.IsEmpty(coord))
            {
                return false;
            }

            if (!ValidateObjectOccupancy(coord) || !ValidateSupportRequirements(coord, tileId))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Evaluates whether a tile break operation would currently succeed.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="context">Break metadata and behavior flags.</param>
    /// <returns><see langword="true"/> when the break would currently succeed.</returns>
    public bool CanBreakTile(WorldTileCoord coord, TileBreakContext context)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return false;
        }

        EnsureChunkLoadedForRead(coord);

        if (!TryGetExistingTile(coord, out var previousTileId))
        {
            return false;
        }

        if (context.IgnoreHardness)
        {
            return true;
        }

        return _contentRegistry.TryGetTileDef(previousTileId, out var tileDef) && tileDef.CanBeMined;
    }

    private TileEditResult SetForegroundTileCore(
        WorldTileCoord coord,
        ushort newTileId,
        bool publishSemanticEvent,
        TilePlacementContext semanticPlacementContext,
        TileBreakContext semanticBreakContext,
        bool suppressEvents)
    {
        var chunk = ResolveChunkForWrite(coord, newTileId);
        if (chunk is null)
        {
            return TileEditResult.Failed(TileEditErrorCode.NoTilePresent, coord);
        }

        var localCoord = _worldQueryService.ToLocalCoord(coord);
        var existingCell = chunk.GetCell(localCoord.X, localCoord.Y);
        var previousTileId = existingCell.ForegroundTileId;

        if (previousTileId == newTileId)
        {
            return TileEditResult.Succeeded(coord, previousTileId, newTileId, ChunkDirtyFlags.None);
        }

        var oldWasSolid = TryGetTileDef(previousTileId, out var previousTileDef) && previousTileDef.IsSolid;
        var newIsSolid = TryGetTileDef(newTileId, out var newTileDef) && newTileDef.IsSolid;

        var updatedCell = existingCell with
        {
            ForegroundTileId = newTileId,
            Variant = semanticPlacementContext?.VariantHint ?? existingCell.Variant
        };

        chunk.SetCell(localCoord.X, localCoord.Y, updatedCell);

        var dirtyFlags = ChunkDirtyFlags.RenderDirty | ChunkDirtyFlags.SaveDirty | ChunkDirtyFlags.AutoTileDirty | ChunkDirtyFlags.LightDirty;
        if (oldWasSolid != newIsSolid)
        {
            dirtyFlags |= ChunkDirtyFlags.CollisionDirty;
        }

        _dirtyTracker.MarkDirty(chunk.Coord, dirtyFlags);

        var neighborDirtyFlags = ChunkDirtyFlags.RenderDirty | ChunkDirtyFlags.AutoTileDirty;
        if (oldWasSolid != newIsSolid)
        {
            neighborDirtyFlags |= ChunkDirtyFlags.CollisionDirty;
            dirtyFlags |= ChunkDirtyFlags.CollisionDirty;
        }

        _dirtyTracker.MarkNeighborDirtyIfBoundary(coord, neighborDirtyFlags);
        _lightingSystem?.MarkDirty(coord);
        _autoTileSystem.RefreshAround(coord);
        _supportSystem?.RefreshAfterTileChanged(coord);

        var result = TileEditResult.Succeeded(coord, previousTileId, newTileId, dirtyFlags);

        if (_entityManager is not null &&
            semanticBreakContext is not null &&
            semanticBreakContext.SpawnDrops &&
            previousTileId != 0 &&
            TryGetTileDef(previousTileId, out var removedTileDef) &&
            removedTileDef.BreakDropItemId != 0)
        {
            _entityManager.SpawnDrop(
                removedTileDef.BreakDropItemId,
                new TileWorld.Engine.Core.Math.Float2(coord.X + 0.5f, coord.Y + 0.5f));
        }

        if (!suppressEvents)
        {
            _eventBus.Publish(new TileChangedEvent(coord, previousTileId, newTileId));

            if (publishSemanticEvent)
            {
                if (semanticPlacementContext is not null)
                {
                    _eventBus.Publish(new TilePlacedEvent(
                        coord,
                        newTileId,
                        semanticPlacementContext.Source,
                        semanticPlacementContext.ActorEntityId));
                }

                if (semanticBreakContext is not null)
                {
                    _eventBus.Publish(new TileBrokenEvent(
                        coord,
                        previousTileId,
                        semanticBreakContext.Source,
                        semanticBreakContext.ActorEntityId));
                }
            }
        }

        return result;
    }

    private bool SetBackgroundWallCore(WorldTileCoord coord, ushort wallId)
    {
        var chunk = ResolveChunkForWallWrite(coord, wallId);
        if (chunk is null)
        {
            return false;
        }

        var localCoord = _worldQueryService.ToLocalCoord(coord);
        var existingCell = chunk.GetCell(localCoord.X, localCoord.Y);
        if (existingCell.BackgroundWallId == wallId)
        {
            return true;
        }

        chunk.SetCell(localCoord.X, localCoord.Y, existingCell with { BackgroundWallId = wallId });
        _dirtyTracker.MarkDirty(chunk.Coord, ChunkDirtyFlags.RenderDirty | ChunkDirtyFlags.SaveDirty | ChunkDirtyFlags.LightDirty);
        _lightingSystem?.MarkDirty(coord);
        return true;
    }

    private World.Chunks.Chunk ResolveChunkForWrite(WorldTileCoord coord, ushort newTileId)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return null;
        }

        var chunkCoord = _worldQueryService.ToChunkCoord(coord);

        if (_worldData.TryGetChunk(chunkCoord, out var existingChunk))
        {
            return existingChunk;
        }

        if (newTileId == 0)
        {
            return null;
        }

        return _chunkManager is not null
            ? _chunkManager.GetOrLoadChunk(chunkCoord)
            : _worldData.GetOrCreateChunk(chunkCoord);
    }

    private World.Chunks.Chunk ResolveChunkForWallWrite(WorldTileCoord coord, ushort newWallId)
    {
        if (!IsWithinWorldBounds(coord))
        {
            return null;
        }

        var chunkCoord = _worldQueryService.ToChunkCoord(coord);

        if (_worldData.TryGetChunk(chunkCoord, out var existingChunk))
        {
            return existingChunk;
        }

        if (newWallId == 0)
        {
            return null;
        }

        return _chunkManager is not null
            ? _chunkManager.GetOrLoadChunk(chunkCoord)
            : _worldData.GetOrCreateChunk(chunkCoord);
    }

    private bool TryGetExistingTile(WorldTileCoord coord, out ushort previousTileId)
    {
        var cell = _worldQueryService.GetCell(coord);
        previousTileId = cell.ForegroundTileId;
        return previousTileId != 0;
    }

    private ushort GetCurrentTileId(WorldTileCoord coord)
    {
        return _worldQueryService.GetCell(coord).ForegroundTileId;
    }

    private bool TryGetTileDef(ushort tileId, out TileDef tileDef)
    {
        if (tileId == 0)
        {
            tileDef = null!;
            return false;
        }

        return _contentRegistry.TryGetTileDef(tileId, out tileDef);
    }

    private bool ValidateObjectOccupancy(WorldTileCoord coord)
    {
        return _objectManager is null || !_objectManager.IsOccupied(coord);
    }

    private static bool ValidateSupportRequirements(WorldTileCoord coord, ushort tileId)
    {
        return true;
    }

    private void EnsureChunkLoadedForRead(WorldTileCoord coord)
    {
        if (_chunkManager is not null && IsWithinWorldBounds(coord))
        {
            _chunkManager.GetOrLoadChunk(_worldQueryService.ToChunkCoord(coord));
        }
    }

    private bool IsWithinWorldBounds(WorldTileCoord coord)
    {
        return _worldQueryService.IsWithinWorldBounds(coord);
    }
}
