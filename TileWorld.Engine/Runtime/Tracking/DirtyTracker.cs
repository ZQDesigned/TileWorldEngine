using System.Collections.Generic;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Tracking;

/// <summary>
/// Centralizes chunk dirty-flag writes and queries.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="WorldRuntime"/> instead of taking a
/// direct dependency on chunk dirty-flag orchestration.
/// </remarks>
internal sealed class DirtyTracker
{
    private readonly WorldData _worldData;

    /// <summary>
    /// Creates a dirty tracker over the supplied world data.
    /// </summary>
    /// <param name="worldData">The world data whose chunk dirty flags should be managed.</param>
    public DirtyTracker(WorldData worldData)
    {
        _worldData = worldData;
    }

    /// <summary>
    /// Applies dirty flags to a chunk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to mark.</param>
    /// <param name="flags">The dirty flags to apply.</param>
    public void MarkDirty(ChunkCoord coord, ChunkDirtyFlags flags)
    {
        var chunk = _worldData.GetOrCreateChunk(coord);
        chunk.DirtyFlags |= flags;
    }

    /// <summary>
    /// Marks a chunk as needing its render cache rebuilt.
    /// </summary>
    /// <param name="coord">The chunk coordinate to mark.</param>
    public void MarkRenderDirty(ChunkCoord coord)
    {
        MarkDirty(coord, ChunkDirtyFlags.RenderDirty);
    }

    /// <summary>
    /// Marks a chunk as needing collision data refreshed.
    /// </summary>
    /// <param name="coord">The chunk coordinate to mark.</param>
    public void MarkCollisionDirty(ChunkCoord coord)
    {
        MarkDirty(coord, ChunkDirtyFlags.CollisionDirty);
    }

    /// <summary>
    /// Marks a chunk as needing persistence.
    /// </summary>
    /// <param name="coord">The chunk coordinate to mark.</param>
    public void MarkSaveDirty(ChunkCoord coord)
    {
        MarkDirty(coord, ChunkDirtyFlags.SaveDirty);
    }

    /// <summary>
    /// Marks a chunk as needing autotile refresh tracking.
    /// </summary>
    /// <param name="coord">The chunk coordinate to mark.</param>
    public void MarkAutoTileDirty(ChunkCoord coord)
    {
        MarkDirty(coord, ChunkDirtyFlags.AutoTileDirty);
    }

    /// <summary>
    /// Propagates dirty flags to orthogonal neighbor chunks when a tile on a chunk boundary changes.
    /// </summary>
    /// <param name="coord">The world-tile coordinate that changed.</param>
    /// <param name="flags">The flags to apply to loaded neighbor chunks.</param>
    public void MarkNeighborDirtyIfBoundary(WorldTileCoord coord, ChunkDirtyFlags flags)
    {
        var chunkCoord = WorldCoordinateConverter.ToChunkCoord(coord);
        var localCoord = WorldCoordinateConverter.ToLocalCoord(coord);

        if (localCoord.Y == 0)
        {
            TryMarkNeighbor(chunkCoord.Offset(0, -1), flags);
        }

        if (localCoord.X == ChunkDimensions.Width - 1)
        {
            TryMarkNeighbor(chunkCoord.Offset(1, 0), flags);
        }

        if (localCoord.Y == ChunkDimensions.Height - 1)
        {
            TryMarkNeighbor(chunkCoord.Offset(0, 1), flags);
        }

        if (localCoord.X == 0)
        {
            TryMarkNeighbor(chunkCoord.Offset(-1, 0), flags);
        }
    }

    /// <summary>
    /// Enumerates loaded chunks that match the supplied dirty-flag mask.
    /// </summary>
    /// <param name="flags">The dirty-flag mask to test.</param>
    /// <returns>The matching chunk coordinates.</returns>
    public IEnumerable<ChunkCoord> EnumerateDirtyChunks(ChunkDirtyFlags flags)
    {
        foreach (var chunk in _worldData.EnumerateLoadedChunks())
        {
            if ((chunk.DirtyFlags & flags) != ChunkDirtyFlags.None)
            {
                yield return chunk.Coord;
            }
        }
    }

    /// <summary>
    /// Returns whether a chunk currently matches a dirty-flag mask.
    /// </summary>
    /// <param name="coord">The chunk coordinate to inspect.</param>
    /// <param name="flags">The dirty-flag mask to test.</param>
    /// <returns><see langword="true"/> when the chunk matches the supplied mask.</returns>
    public bool HasDirty(ChunkCoord coord, ChunkDirtyFlags flags)
    {
        return _worldData.TryGetChunk(coord, out var chunk) &&
               (chunk.DirtyFlags & flags) != ChunkDirtyFlags.None;
    }

    /// <summary>
    /// Clears dirty flags from a chunk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to update.</param>
    /// <param name="flags">The flags to clear.</param>
    public void ClearDirty(ChunkCoord coord, ChunkDirtyFlags flags)
    {
        if (_worldData.TryGetChunk(coord, out var chunk))
        {
            chunk.DirtyFlags &= ~flags;
        }
    }

    private void TryMarkNeighbor(ChunkCoord coord, ChunkDirtyFlags flags)
    {
        if (_worldData.HasChunk(coord))
        {
            MarkDirty(coord, flags);
        }
    }
}
