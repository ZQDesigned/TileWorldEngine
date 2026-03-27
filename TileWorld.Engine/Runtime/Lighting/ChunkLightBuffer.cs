using System;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Lighting;

/// <summary>
/// Stores derived per-tile light levels for a loaded chunk.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="Runtime.WorldRuntime"/> instead of
/// depending on transient chunk lighting caches directly.
/// </remarks>
internal sealed class ChunkLightBuffer
{
    private readonly byte[] _lightLevels = new byte[ChunkDimensions.CellCount];

    /// <summary>
    /// Creates a chunk light buffer for the supplied chunk coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate represented by this buffer.</param>
    public ChunkLightBuffer(ChunkCoord coord)
    {
        Coord = coord;
    }

    /// <summary>
    /// Gets the chunk coordinate represented by this light buffer.
    /// </summary>
    public ChunkCoord Coord { get; }

    /// <summary>
    /// Gets the light level at a local chunk coordinate.
    /// </summary>
    /// <param name="localX">The local chunk X coordinate.</param>
    /// <param name="localY">The local chunk Y coordinate.</param>
    /// <returns>The stored light level.</returns>
    public byte GetLightLevel(int localX, int localY)
    {
        return _lightLevels[WorldCoordinateConverter.ToIndex(localX, localY)];
    }

    /// <summary>
    /// Writes the light level at a local chunk coordinate.
    /// </summary>
    /// <param name="localX">The local chunk X coordinate.</param>
    /// <param name="localY">The local chunk Y coordinate.</param>
    /// <param name="lightLevel">The light level to store.</param>
    public void SetLightLevel(int localX, int localY, byte lightLevel)
    {
        _lightLevels[WorldCoordinateConverter.ToIndex(localX, localY)] = lightLevel;
    }

    /// <summary>
    /// Clears the buffer to darkness.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_lightLevels);
    }
}
