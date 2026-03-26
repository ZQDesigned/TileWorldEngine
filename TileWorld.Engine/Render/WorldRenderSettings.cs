using System;
using TileWorld.Engine.World.Chunks;

namespace TileWorld.Engine.Render;

/// <summary>
/// Defines shared rendering constants for tile and chunk presentation.
/// </summary>
public sealed class WorldRenderSettings
{
    /// <summary>
    /// Creates render settings using the default tile size and visible chunk padding.
    /// </summary>
    public WorldRenderSettings()
        : this(tileSizePixels: 16, visibleChunkPadding: 1)
    {
    }

    /// <summary>
    /// Creates render settings with explicit tile size and visible chunk padding values.
    /// </summary>
    /// <param name="tileSizePixels">The size of one tile in screen pixels.</param>
    /// <param name="visibleChunkPadding">The number of extra chunks to include around the camera view.</param>
    public WorldRenderSettings(int tileSizePixels, int visibleChunkPadding)
    {
        if (tileSizePixels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tileSizePixels), tileSizePixels, "Tile size must be positive.");
        }

        if (visibleChunkPadding < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(visibleChunkPadding), visibleChunkPadding, "Visible chunk padding cannot be negative.");
        }

        TileSizePixels = tileSizePixels;
        VisibleChunkPadding = visibleChunkPadding;
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
    /// Gets the width of one chunk in pixels.
    /// </summary>
    public int ChunkWidthPixels => ChunkDimensions.Width * TileSizePixels;

    /// <summary>
    /// Gets the height of one chunk in pixels.
    /// </summary>
    public int ChunkHeightPixels => ChunkDimensions.Height * TileSizePixels;
}
