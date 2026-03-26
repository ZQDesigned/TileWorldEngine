using System;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.World.Chunks;

namespace TileWorld.Engine.World.Coordinates;

/// <summary>
/// Converts between world-tile, chunk, and local chunk coordinate systems.
/// </summary>
public static class WorldCoordinateConverter
{
    /// <summary>
    /// Converts a world-tile coordinate into the containing chunk coordinate using floor semantics for negative values.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to convert.</param>
    /// <returns>The containing chunk coordinate.</returns>
    public static ChunkCoord ToChunkCoord(WorldTileCoord coord)
    {
        return new ChunkCoord(
            FloorDivide(coord.X, ChunkDimensions.Width),
            FloorDivide(coord.Y, ChunkDimensions.Height));
    }

    /// <summary>
    /// Converts a world-tile coordinate into its local coordinate inside the containing chunk.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to convert.</param>
    /// <returns>The local chunk-space coordinate.</returns>
    public static Int2 ToLocalCoord(WorldTileCoord coord)
    {
        var chunkCoord = ToChunkCoord(coord);
        var origin = ToChunkOrigin(chunkCoord);

        return new Int2(coord.X - origin.X, coord.Y - origin.Y);
    }

    /// <summary>
    /// Converts a chunk coordinate into the world-tile origin of that chunk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to convert.</param>
    /// <returns>The world-tile origin of the chunk.</returns>
    public static WorldTileCoord ToChunkOrigin(ChunkCoord coord)
    {
        return new WorldTileCoord(coord.X * ChunkDimensions.Width, coord.Y * ChunkDimensions.Height);
    }

    /// <summary>
    /// Converts local chunk coordinates into a row-major cell index.
    /// </summary>
    /// <param name="localX">The local chunk X coordinate.</param>
    /// <param name="localY">The local chunk Y coordinate.</param>
    /// <returns>The row-major index into a chunk cell buffer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the supplied local coordinate is outside chunk bounds.</exception>
    public static int ToIndex(int localX, int localY)
    {
        ValidateLocalCoordinates(localX, localY);

        return (localY * ChunkDimensions.Width) + localX;
    }

    /// <summary>
    /// Returns whether the supplied local chunk coordinate lies inside the fixed chunk bounds.
    /// </summary>
    /// <param name="localX">The local chunk X coordinate.</param>
    /// <param name="localY">The local chunk Y coordinate.</param>
    /// <returns><see langword="true"/> when the coordinate is inside the chunk.</returns>
    public static bool IsInsideLocal(int localX, int localY)
    {
        return localX >= 0 &&
               localX < ChunkDimensions.Width &&
               localY >= 0 &&
               localY < ChunkDimensions.Height;
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;

        if (remainder < 0)
        {
            quotient--;
        }

        return quotient;
    }

    private static void ValidateLocalCoordinates(int localX, int localY)
    {
        if (!IsInsideLocal(localX, localY))
        {
            throw new ArgumentOutOfRangeException(
                $"Local coordinates ({localX}, {localY}) are outside the valid chunk bounds.");
        }
    }
}
