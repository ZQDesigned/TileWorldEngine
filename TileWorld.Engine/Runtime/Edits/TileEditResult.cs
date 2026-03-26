using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Edits;

/// <summary>
/// Represents the outcome of a tile editing operation.
/// </summary>
/// <param name="Success">Whether the edit operation completed successfully.</param>
/// <param name="ErrorCode">The failure reason when <paramref name="Success"/> is <see langword="false"/>.</param>
/// <param name="Coord">The world-tile coordinate targeted by the edit.</param>
/// <param name="PreviousTileId">The foreground tile identifier before the edit.</param>
/// <param name="CurrentTileId">The foreground tile identifier after the edit.</param>
/// <param name="DirtyFlagsApplied">The dirty flags applied as part of the edit.</param>
public readonly record struct TileEditResult(
    bool Success,
    TileEditErrorCode ErrorCode,
    WorldTileCoord Coord,
    ushort PreviousTileId,
    ushort CurrentTileId,
    ChunkDirtyFlags DirtyFlagsApplied)
{
    /// <summary>
    /// Creates a failed tile-edit result.
    /// </summary>
    /// <param name="errorCode">The failure reason.</param>
    /// <param name="coord">The world-tile coordinate targeted by the edit.</param>
    /// <param name="previousTileId">The foreground tile identifier that was present before the failed edit.</param>
    /// <returns>A failed tile-edit result.</returns>
    public static TileEditResult Failed(TileEditErrorCode errorCode, WorldTileCoord coord, ushort previousTileId = 0)
    {
        return new TileEditResult(false, errorCode, coord, previousTileId, previousTileId, ChunkDirtyFlags.None);
    }

    /// <summary>
    /// Creates a successful tile-edit result.
    /// </summary>
    /// <param name="coord">The world-tile coordinate targeted by the edit.</param>
    /// <param name="previousTileId">The foreground tile identifier before the edit.</param>
    /// <param name="currentTileId">The foreground tile identifier after the edit.</param>
    /// <param name="dirtyFlagsApplied">The dirty flags applied as part of the edit.</param>
    /// <returns>A successful tile-edit result.</returns>
    public static TileEditResult Succeeded(
        WorldTileCoord coord,
        ushort previousTileId,
        ushort currentTileId,
        ChunkDirtyFlags dirtyFlagsApplied)
    {
        return new TileEditResult(true, TileEditErrorCode.None, coord, previousTileId, currentTileId, dirtyFlagsApplied);
    }
}
