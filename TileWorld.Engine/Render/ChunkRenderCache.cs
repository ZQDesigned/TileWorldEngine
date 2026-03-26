using System;
using System.Collections.Generic;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Render;

/// <summary>
/// Stores render commands generated for a single chunk.
/// </summary>
/// <remarks>
/// This API is intended for engine infrastructure and advanced tooling. Most callers should prefer
/// <see cref="WorldRenderer"/> instead of managing chunk render caches directly.
/// </remarks>
public sealed class ChunkRenderCache
{
    /// <summary>
    /// Creates a chunk render cache snapshot.
    /// </summary>
    /// <param name="coord">The chunk coordinate represented by the cache.</param>
    /// <param name="isBuilt">Whether the cache contains valid draw commands.</param>
    /// <param name="lastBuildTick">The monotonic build tick that produced the cache.</param>
    /// <param name="worldPixelBounds">The world-space pixel bounds covered by the chunk.</param>
    /// <param name="backgroundCommands">The background-wall draw commands generated for the chunk.</param>
    /// <param name="foregroundCommands">The foreground draw commands generated for the chunk.</param>
    public ChunkRenderCache(
        ChunkCoord coord,
        bool isBuilt,
        int lastBuildTick,
        RectI worldPixelBounds,
        IReadOnlyList<SpriteDrawCommand> backgroundCommands,
        IReadOnlyList<SpriteDrawCommand> foregroundCommands)
    {
        ArgumentNullException.ThrowIfNull(backgroundCommands);
        ArgumentNullException.ThrowIfNull(foregroundCommands);

        Coord = coord;
        IsBuilt = isBuilt;
        LastBuildTick = lastBuildTick;
        WorldPixelBounds = worldPixelBounds;
        BackgroundCommands = backgroundCommands;
        ForegroundCommands = foregroundCommands;
    }

    /// <summary>
    /// Gets the chunk coordinate represented by this cache.
    /// </summary>
    public ChunkCoord Coord { get; }

    /// <summary>
    /// Gets a value indicating whether the cache contains valid draw commands.
    /// </summary>
    public bool IsBuilt { get; }

    /// <summary>
    /// Gets the monotonic build tick that produced this cache.
    /// </summary>
    public int LastBuildTick { get; }

    /// <summary>
    /// Gets the world-space pixel bounds covered by the chunk.
    /// </summary>
    public RectI WorldPixelBounds { get; }

    /// <summary>
    /// Gets the background-wall draw commands generated for the chunk.
    /// </summary>
    public IReadOnlyList<SpriteDrawCommand> BackgroundCommands { get; }

    /// <summary>
    /// Gets the foreground draw commands generated for the chunk.
    /// </summary>
    public IReadOnlyList<SpriteDrawCommand> ForegroundCommands { get; }
}
