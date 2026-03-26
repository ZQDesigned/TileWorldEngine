using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.AutoTile;

/// <summary>
/// Updates tile variants based on neighboring autotile group connectivity.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="Runtime.WorldRuntime"/> and let
/// the runtime coordinate autotile refreshes automatically after world edits.
/// </remarks>
internal sealed class AutoTileSystem
{
    private readonly WorldData _worldData;
    private readonly WorldQueryService _worldQueryService;

    /// <summary>
    /// Creates an autotile system over the supplied world data and query service.
    /// </summary>
    /// <param name="worldData">The world data whose chunks will receive variant updates.</param>
    /// <param name="worldQueryService">The query service used to inspect neighboring tiles.</param>
    public AutoTileSystem(WorldData worldData, WorldQueryService worldQueryService)
    {
        _worldData = worldData;
        _worldQueryService = worldQueryService;
    }

    /// <summary>
    /// Refreshes the autotile variant at a single world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to refresh.</param>
    public void RefreshAt(WorldTileCoord coord)
    {
        if (!TryGetMutableCell(coord, out var chunk, out var localCoord, out var cell))
        {
            return;
        }

        var updatedCell = cell with { Variant = ComputeVariant(coord) };
        chunk.SetCell(localCoord.X, localCoord.Y, updatedCell);
    }

    /// <summary>
    /// Refreshes the autotile variant at the supplied coordinate and its four orthogonal neighbors.
    /// </summary>
    /// <param name="center">The center coordinate to refresh around.</param>
    public void RefreshAround(WorldTileCoord center)
    {
        RefreshAt(center);

        foreach (var neighbor in _worldQueryService.EnumerateNeighbors4(center))
        {
            RefreshAt(neighbor);
        }
    }

    /// <summary>
    /// Refreshes autotile variants for every tile inside a world-tile rectangle.
    /// </summary>
    /// <param name="area">The world-tile rectangle to refresh.</param>
    public void RefreshArea(RectI area)
    {
        foreach (var coord in _worldQueryService.EnumerateArea(area))
        {
            RefreshAt(new WorldTileCoord(coord.X, coord.Y));
        }
    }

    /// <summary>
    /// Computes the autotile variant value for a single coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to evaluate.</param>
    /// <returns>The autotile variant value, or <c>0</c> when autotiling does not apply.</returns>
    public ushort ComputeVariant(WorldTileCoord coord)
    {
        if (!TryGetForegroundTileDef(coord, out var tileDef) || tileDef.AutoTileGroupId == 0)
        {
            return 0;
        }

        return ComputeNeighborMask(coord);
    }

    /// <summary>
    /// Computes the raw four-neighbor connectivity mask for a single coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to evaluate.</param>
    /// <returns>The up-right-down-left connectivity bitmask.</returns>
    public ushort ComputeNeighborMask(WorldTileCoord coord)
    {
        if (!TryGetForegroundTileDef(coord, out var tileDef) || tileDef.AutoTileGroupId == 0)
        {
            return 0;
        }

        ushort mask = 0;

        if (BelongsToSameAutoTileGroup(coord.Offset(0, -1), tileDef))
        {
            mask |= 1;
        }

        if (BelongsToSameAutoTileGroup(coord.Offset(1, 0), tileDef))
        {
            mask |= 2;
        }

        if (BelongsToSameAutoTileGroup(coord.Offset(0, 1), tileDef))
        {
            mask |= 4;
        }

        if (BelongsToSameAutoTileGroup(coord.Offset(-1, 0), tileDef))
        {
            mask |= 8;
        }

        return mask;
    }

    private bool BelongsToSameAutoTileGroup(WorldTileCoord coord, TileDef tileDef)
    {
        return TryGetForegroundTileDef(coord, out var neighborTileDef) &&
               neighborTileDef.AutoTileGroupId != 0 &&
               neighborTileDef.AutoTileGroupId == tileDef.AutoTileGroupId;
    }

    private bool TryGetForegroundTileDef(WorldTileCoord coord, out TileDef tileDef)
    {
        return _worldQueryService.TryGetForegroundTileDef(coord, out tileDef);
    }

    private bool TryGetMutableCell(
        WorldTileCoord coord,
        out World.Chunks.Chunk chunk,
        out Int2 localCoord,
        out TileCell cell)
    {
        var chunkCoord = _worldQueryService.ToChunkCoord(coord);
        localCoord = _worldQueryService.ToLocalCoord(coord);

        if (!_worldData.TryGetChunk(chunkCoord, out chunk))
        {
            cell = TileCell.Empty;
            return false;
        }

        cell = chunk.GetCell(localCoord.X, localCoord.Y);
        return true;
    }
}
