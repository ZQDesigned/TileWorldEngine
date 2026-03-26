using System;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World.Chunks;

/// <summary>
/// Provides safe indexed access over the fixed-size cell array owned by a chunk.
/// </summary>
public sealed class ChunkCellStorage
{
    private readonly TileCell[] _cells;

    /// <summary>
    /// Creates an empty fixed-size chunk cell buffer.
    /// </summary>
    public ChunkCellStorage()
    {
        _cells = new TileCell[ChunkDimensions.CellCount];
    }

    /// <summary>
    /// Gets the total number of cells stored by the buffer.
    /// </summary>
    public int Count => _cells.Length;

    /// <summary>
    /// Returns whether the supplied local coordinate lies inside the chunk.
    /// </summary>
    /// <param name="localX">The local X coordinate.</param>
    /// <param name="localY">The local Y coordinate.</param>
    /// <returns><see langword="true"/> when the coordinate is inside the buffer bounds.</returns>
    public bool IsInside(int localX, int localY)
    {
        return WorldCoordinateConverter.IsInsideLocal(localX, localY);
    }

    /// <summary>
    /// Converts a local chunk coordinate into a row-major cell index.
    /// </summary>
    /// <param name="localX">The local X coordinate.</param>
    /// <param name="localY">The local Y coordinate.</param>
    /// <returns>The row-major cell index.</returns>
    public int ToIndex(int localX, int localY)
    {
        return WorldCoordinateConverter.ToIndex(localX, localY);
    }

    /// <summary>
    /// Reads a cell from local chunk coordinates.
    /// </summary>
    /// <param name="localX">The local X coordinate.</param>
    /// <param name="localY">The local Y coordinate.</param>
    /// <returns>The resolved cell value.</returns>
    public TileCell GetCell(int localX, int localY)
    {
        return _cells[ToIndex(localX, localY)];
    }

    /// <summary>
    /// Writes a cell to local chunk coordinates.
    /// </summary>
    /// <param name="localX">The local X coordinate.</param>
    /// <param name="localY">The local Y coordinate.</param>
    /// <param name="cell">The cell value to store.</param>
    public void SetCell(int localX, int localY, TileCell cell)
    {
        _cells[ToIndex(localX, localY)] = cell;
    }

    /// <summary>
    /// Returns a by-reference view of a cell inside the buffer.
    /// </summary>
    /// <param name="localX">The local X coordinate.</param>
    /// <param name="localY">The local Y coordinate.</param>
    /// <returns>A writable reference to the cell.</returns>
    /// <remarks>
    /// Engine internal infrastructure API. External callers should use <see cref="GetCell"/> and <see cref="SetCell"/>
    /// instead of by-reference mutation.
    /// </remarks>
    internal ref TileCell GetCellRef(int localX, int localY)
    {
        return ref _cells[ToIndex(localX, localY)];
    }
}
