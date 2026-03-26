using System;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World.Chunks;

/// <summary>
/// Stores cell data and runtime state for a single chunk.
/// </summary>
public sealed class Chunk
{
    /// <summary>
    /// Creates a chunk with a fresh cell buffer at the supplied coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    public Chunk(ChunkCoord coord)
        : this(coord, new ChunkCellStorage())
    {
    }

    /// <summary>
    /// Creates a chunk with an explicit cell buffer.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="cellStorage">The cell storage backing the chunk.</param>
    public Chunk(ChunkCoord coord, ChunkCellStorage cellStorage)
    {
        ArgumentNullException.ThrowIfNull(cellStorage);

        Coord = coord;
        CellStorage = cellStorage;
        State = ChunkState.Loaded;
        DirtyFlags = ChunkDirtyFlags.None;
    }

    /// <summary>
    /// Gets the coordinate of the chunk.
    /// </summary>
    public ChunkCoord Coord { get; }

    /// <summary>
    /// Gets the cell storage backing this chunk.
    /// </summary>
    public ChunkCellStorage CellStorage { get; }

    /// <summary>
    /// Gets or sets the chunk lifecycle state.
    /// </summary>
    public ChunkState State { get; set; }

    /// <summary>
    /// Gets or sets the dirty flags currently applied to the chunk.
    /// </summary>
    public ChunkDirtyFlags DirtyFlags { get; set; }

    /// <summary>
    /// Returns whether the supplied local coordinate lies inside the chunk.
    /// </summary>
    /// <param name="localX">The local X coordinate.</param>
    /// <param name="localY">The local Y coordinate.</param>
    /// <returns><see langword="true"/> when the coordinate is inside the chunk.</returns>
    public bool IsInside(int localX, int localY)
    {
        return CellStorage.IsInside(localX, localY);
    }

    /// <summary>
    /// Converts a local chunk coordinate into a row-major cell index.
    /// </summary>
    /// <param name="localX">The local X coordinate.</param>
    /// <param name="localY">The local Y coordinate.</param>
    /// <returns>The row-major index into the chunk cell buffer.</returns>
    public int ToIndex(int localX, int localY)
    {
        return CellStorage.ToIndex(localX, localY);
    }

    /// <summary>
    /// Reads a cell from local chunk coordinates.
    /// </summary>
    /// <param name="localX">The local X coordinate.</param>
    /// <param name="localY">The local Y coordinate.</param>
    /// <returns>The resolved cell value.</returns>
    public TileCell GetCell(int localX, int localY)
    {
        return CellStorage.GetCell(localX, localY);
    }

    /// <summary>
    /// Writes a cell to local chunk coordinates.
    /// </summary>
    /// <param name="localX">The local X coordinate.</param>
    /// <param name="localY">The local Y coordinate.</param>
    /// <param name="cell">The cell value to store.</param>
    public void SetCell(int localX, int localY, TileCell cell)
    {
        CellStorage.SetCell(localX, localY, cell);
    }
}
