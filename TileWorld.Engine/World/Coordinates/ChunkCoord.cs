namespace TileWorld.Engine.World.Coordinates;

/// <summary>
/// Represents chunk-space coordinates.
/// </summary>
/// <param name="X">The chunk coordinate on the horizontal axis.</param>
/// <param name="Y">The chunk coordinate on the vertical axis.</param>
public readonly record struct ChunkCoord(int X, int Y)
{
    /// <summary>
    /// Creates a new chunk coordinate offset from the current one.
    /// </summary>
    /// <param name="dx">The horizontal chunk delta.</param>
    /// <param name="dy">The vertical chunk delta.</param>
    /// <returns>The offset chunk coordinate.</returns>
    public ChunkCoord Offset(int dx, int dy)
    {
        return new ChunkCoord(X + dx, Y + dy);
    }

    /// <summary>
    /// Returns a debugger-friendly textual representation of the coordinate.
    /// </summary>
    /// <returns>A formatted chunk coordinate string.</returns>
    public override string ToString()
    {
        return $"ChunkCoord({X}, {Y})";
    }
}
