using System;
using TileWorld.Engine.World.Chunks;

namespace TileWorld.Engine.Render;

/// <summary>
/// Defines shared rendering constants for tile and chunk presentation.
/// </summary>
public sealed class WorldRenderSettings
{
    /// <summary>
    /// Creates render settings using the default tile size, visible chunk padding, and cache rebuild budget.
    /// </summary>
    public WorldRenderSettings()
        : this(tileSizePixels: 16, visibleChunkPadding: 1, maxDirtyChunkCacheRebuildsPerFrame: 6)
    {
    }

    /// <summary>
    /// Creates render settings with explicit tile size, visible chunk padding, and cache rebuild budget values.
    /// </summary>
    /// <param name="tileSizePixels">The size of one tile in screen pixels.</param>
    /// <param name="visibleChunkPadding">The number of extra chunks to include around the camera view.</param>
    /// <param name="maxDirtyChunkCacheRebuildsPerFrame">The maximum number of dirty chunk render caches rebuilt in a single frame.</param>
    public WorldRenderSettings(int tileSizePixels, int visibleChunkPadding, int maxDirtyChunkCacheRebuildsPerFrame = 6)
    {
        if (tileSizePixels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tileSizePixels), tileSizePixels, "Tile size must be positive.");
        }

        if (visibleChunkPadding < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(visibleChunkPadding), visibleChunkPadding, "Visible chunk padding cannot be negative.");
        }

        if (maxDirtyChunkCacheRebuildsPerFrame <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDirtyChunkCacheRebuildsPerFrame),
                maxDirtyChunkCacheRebuildsPerFrame,
                "The cache rebuild budget must be positive.");
        }

        TileSizePixels = tileSizePixels;
        VisibleChunkPadding = visibleChunkPadding;
        MaxDirtyChunkCacheRebuildsPerFrame = maxDirtyChunkCacheRebuildsPerFrame;
    }

    /// <summary>
    /// Gets the size of one tile in screen pixels.
    /// </summary>
    public int TileSizePixels { get; }

    /// <summary>
    /// Gets the number of extra chunks to include around the visible camera range.
    /// </summary>
    public int VisibleChunkPadding { get; }

    /// <summary>
    /// Gets the maximum number of dirty chunk render caches rebuilt during one frame.
    /// </summary>
    public int MaxDirtyChunkCacheRebuildsPerFrame { get; }

    /// <summary>
    /// Gets the width of one chunk in pixels.
    /// </summary>
    public int ChunkWidthPixels => ChunkDimensions.Width * TileSizePixels;

    /// <summary>
    /// Gets the height of one chunk in pixels.
    /// </summary>
    public int ChunkHeightPixels => ChunkDimensions.Height * TileSizePixels;
}
