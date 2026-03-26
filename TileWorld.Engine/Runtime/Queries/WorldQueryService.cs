using System;
using System.Collections.Generic;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Objects;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Objects;

namespace TileWorld.Engine.Runtime.Queries;

/// <summary>
/// Provides read-only access patterns over world data and tile definitions.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="WorldRuntime"/> so the stable
/// gameplay-facing query surface remains decoupled from lower-level query plumbing.
/// </remarks>
internal sealed class WorldQueryService
{
    private readonly ContentRegistry _contentRegistry;
    private readonly ChunkManager _chunkManager;
    private ObjectManager _objectManager;
    private readonly WorldData _worldData;

    /// <summary>
    /// Creates a query service over the supplied world data and content registry.
    /// </summary>
    /// <param name="worldData">The world data being queried.</param>
    /// <param name="contentRegistry">The registry used to resolve tile definitions.</param>
    /// <param name="objectManager">The optional object manager used to resolve object occupancy.</param>
    /// <param name="chunkManager">The optional chunk manager used when missing chunks may be loaded on demand.</param>
    public WorldQueryService(
        WorldData worldData,
        ContentRegistry contentRegistry,
        ObjectManager objectManager = null,
        ChunkManager chunkManager = null)
    {
        _worldData = worldData;
        _contentRegistry = contentRegistry;
        _objectManager = objectManager;
        _chunkManager = chunkManager;
    }

    /// <summary>
    /// Creates a query service over the supplied world data and content registry with an explicit chunk manager.
    /// </summary>
    /// <param name="worldData">The world data being queried.</param>
    /// <param name="contentRegistry">The registry used to resolve tile definitions.</param>
    /// <param name="chunkManager">The optional chunk manager used when missing chunks may be loaded on demand.</param>
    public WorldQueryService(WorldData worldData, ContentRegistry contentRegistry, ChunkManager chunkManager)
        : this(worldData, contentRegistry, null, chunkManager)
    {
    }

    /// <summary>
    /// Attaches the object manager after runtime construction wiring has completed.
    /// </summary>
    /// <param name="objectManager">The object manager that should serve occupancy queries.</param>
    public void AttachObjectManager(ObjectManager objectManager)
    {
        _objectManager = objectManager;
    }

    /// <summary>
    /// Resolves a cell at world-tile coordinates, optionally loading backing chunk data.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to resolve.</param>
    /// <param name="options">Optional query behavior overrides.</param>
    /// <returns>The resolved cell, or <see cref="TileCell.Empty"/> when no cell data is available.</returns>
    public TileCell GetCell(WorldTileCoord coord, QueryOptions options = null)
    {
        var effectiveOptions = options ?? QueryOptions.Default;

        if (!TryResolveChunk(coord, effectiveOptions, out var chunk, out var localCoord))
        {
            return TileCell.Empty;
        }

        return chunk.GetCell(localCoord.X, localCoord.Y);
    }

    /// <summary>
    /// Attempts to resolve a cell at world-tile coordinates.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to resolve.</param>
    /// <param name="cell">The resolved cell when available.</param>
    /// <param name="options">Optional query behavior overrides.</param>
    /// <returns><see langword="true"/> when cell data was resolved.</returns>
    public bool TryGetCell(WorldTileCoord coord, out TileCell cell, QueryOptions options = null)
    {
        var effectiveOptions = options ?? QueryOptions.Default;

        if (TryResolveChunk(coord, effectiveOptions, out var chunk, out var localCoord))
        {
            cell = chunk.GetCell(localCoord.X, localCoord.Y);
            return true;
        }

        cell = TileCell.Empty;
        return false;
    }

    /// <summary>
    /// Resolves a tile definition by identifier.
    /// </summary>
    /// <param name="tileId">The tile identifier to resolve.</param>
    /// <returns>The resolved tile definition.</returns>
    public TileDef GetTileDef(ushort tileId)
    {
        return _contentRegistry.GetTileDef(tileId);
    }

    /// <summary>
    /// Resolves a wall definition by identifier.
    /// </summary>
    /// <param name="wallId">The wall identifier to resolve.</param>
    /// <returns>The resolved wall definition.</returns>
    public WallDef GetWallDef(ushort wallId)
    {
        return _contentRegistry.GetWallDef(wallId);
    }

    /// <summary>
    /// Attempts to resolve the foreground tile definition at a world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <param name="tileDef">The resolved foreground tile definition when present.</param>
    /// <returns><see langword="true"/> when the coordinate contains a non-air foreground tile.</returns>
    public bool TryGetForegroundTileDef(WorldTileCoord coord, out TileDef tileDef)
    {
        var cell = GetCell(coord);
        if (cell.ForegroundTileId == 0)
        {
            tileDef = null!;
            return false;
        }

        return _contentRegistry.TryGetTileDef(cell.ForegroundTileId, out tileDef);
    }

    /// <summary>
    /// Attempts to resolve the background wall definition at a world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <param name="wallDef">The resolved background wall definition when present.</param>
    /// <returns><see langword="true"/> when the coordinate contains a background wall.</returns>
    public bool TryGetBackgroundWallDef(WorldTileCoord coord, out WallDef wallDef)
    {
        var cell = GetCell(coord);
        if (cell.BackgroundWallId == 0)
        {
            wallDef = null!;
            return false;
        }

        return _contentRegistry.TryGetWallDef(cell.BackgroundWallId, out wallDef);
    }

    /// <summary>
    /// Returns whether the foreground tile at the coordinate is solid.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns><see langword="true"/> when the foreground tile is solid.</returns>
    public bool IsSolid(WorldTileCoord coord)
    {
        return TryGetForegroundTileDef(coord, out var tileDef) && tileDef.IsSolid;
    }

    /// <summary>
    /// Returns whether the foreground tile at the coordinate is air.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns><see langword="true"/> when the cell is empty.</returns>
    public bool IsEmpty(WorldTileCoord coord)
    {
        return GetCell(coord).ForegroundTileId == 0;
    }

    /// <summary>
    /// Returns whether the foreground tile at the coordinate blocks light.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns><see langword="true"/> when the foreground tile blocks light.</returns>
    public bool BlocksLight(WorldTileCoord coord)
    {
        return TryGetForegroundTileDef(coord, out var tileDef) && tileDef.BlocksLight;
    }

    /// <summary>
    /// Returns whether the coordinate contains a background wall.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns><see langword="true"/> when the cell contains a background wall.</returns>
    public bool HasBackgroundWall(WorldTileCoord coord)
    {
        return GetCell(coord).BackgroundWallId != 0;
    }

    /// <summary>
    /// Enumerates the four orthogonal neighbor coordinates around a tile.
    /// </summary>
    /// <param name="coord">The center tile coordinate.</param>
    /// <returns>The orthogonal neighbor coordinates in up, right, down, left order.</returns>
    public IEnumerable<WorldTileCoord> EnumerateNeighbors4(WorldTileCoord coord)
    {
        yield return coord.Offset(0, -1);
        yield return coord.Offset(1, 0);
        yield return coord.Offset(0, 1);
        yield return coord.Offset(-1, 0);
    }

    /// <summary>
    /// Enumerates the eight surrounding neighbor coordinates around a tile.
    /// </summary>
    /// <param name="coord">The center tile coordinate.</param>
    /// <returns>The surrounding neighbor coordinates.</returns>
    public IEnumerable<WorldTileCoord> EnumerateNeighbors8(WorldTileCoord coord)
    {
        yield return coord.Offset(-1, -1);
        yield return coord.Offset(0, -1);
        yield return coord.Offset(1, -1);
        yield return coord.Offset(-1, 0);
        yield return coord.Offset(1, 0);
        yield return coord.Offset(-1, 1);
        yield return coord.Offset(0, 1);
        yield return coord.Offset(1, 1);
    }

    /// <summary>
    /// Enumerates all tile coordinates inside a rectangle.
    /// </summary>
    /// <param name="worldTileRect">The rectangle to enumerate in world-tile space.</param>
    /// <returns>All tile coordinates inside the rectangle.</returns>
    public IEnumerable<Int2> EnumerateArea(RectI worldTileRect)
    {
        for (var y = worldTileRect.Top; y < worldTileRect.Bottom; y++)
        {
            for (var x = worldTileRect.Left; x < worldTileRect.Right; x++)
            {
                yield return new Int2(x, y);
            }
        }
    }

    /// <summary>
    /// Attempts to resolve a loaded chunk by chunk coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate to resolve.</param>
    /// <param name="chunk">The loaded chunk when present.</param>
    /// <returns><see langword="true"/> when the chunk is loaded.</returns>
    public bool TryGetChunk(ChunkCoord coord, out Chunk chunk)
    {
        return _worldData.TryGetChunk(coord, out chunk!);
    }

    /// <summary>
    /// Attempts to resolve a loaded chunk that contains a world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns>The loaded chunk when present; otherwise <see langword="null"/>.</returns>
    public Chunk TryGetChunkByWorldTile(WorldTileCoord coord)
    {
        return _worldData.TryGetChunk(ToChunkCoord(coord), out var chunk) ? chunk : null!;
    }

    /// <summary>
    /// Attempts to resolve a placed object instance at a world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <param name="instance">The resolved object instance when present.</param>
    /// <returns><see langword="true"/> when an object occupies the coordinate.</returns>
    public bool TryGetObjectAt(WorldTileCoord coord, out ObjectInstance instance)
    {
        if (_objectManager is not null && _objectManager.TryGetObjectAt(coord, out instance))
        {
            return true;
        }

        instance = null!;
        return false;
    }

    /// <summary>
    /// Returns whether an object occupies the supplied world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns><see langword="true"/> when an object occupies the coordinate.</returns>
    public bool IsObjectOccupied(WorldTileCoord coord)
    {
        return _objectManager is not null && _objectManager.IsOccupied(coord);
    }

    /// <summary>
    /// Converts a world-tile coordinate into chunk coordinates.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to convert.</param>
    /// <returns>The containing chunk coordinate.</returns>
    public ChunkCoord ToChunkCoord(WorldTileCoord coord)
    {
        return WorldCoordinateConverter.ToChunkCoord(coord);
    }

    /// <summary>
    /// Converts a world-tile coordinate into local chunk coordinates.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to convert.</param>
    /// <returns>The local coordinate inside the containing chunk.</returns>
    public Int2 ToLocalCoord(WorldTileCoord coord)
    {
        return WorldCoordinateConverter.ToLocalCoord(coord);
    }

    private bool TryResolveChunk(
        WorldTileCoord coord,
        QueryOptions options,
        out Chunk chunk,
        out Int2 localCoord)
    {
        var chunkCoord = ToChunkCoord(coord);
        localCoord = ToLocalCoord(coord);

        if (_worldData.TryGetChunk(chunkCoord, out chunk!))
        {
            return options.AllowInactiveChunk || chunk.State != ChunkState.Inactive;
        }

        if (options.LoadChunkIfMissing)
        {
            chunk = _chunkManager is not null
                ? _chunkManager.GetOrLoadChunk(chunkCoord)
                : _worldData.GetOrCreateChunk(chunkCoord);
            return true;
        }

        chunk = null!;
        return false;
    }
}
