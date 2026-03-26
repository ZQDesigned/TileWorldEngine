namespace TileWorld.Engine.World.Coordinates;

/// <summary>
/// Represents world-space tile coordinates.
/// </summary>
/// <param name="X">The tile coordinate on the horizontal axis.</param>
/// <param name="Y">The tile coordinate on the vertical axis.</param>
public readonly record struct WorldTileCoord(int X, int Y)
{
    /// <summary>
    /// Creates a new world-tile coordinate offset from the current one.
    /// </summary>
    /// <param name="dx">The horizontal tile delta.</param>
    /// <param name="dy">The vertical tile delta.</param>
    /// <returns>The offset world-tile coordinate.</returns>
    public WorldTileCoord Offset(int dx, int dy)
    {
        return new WorldTileCoord(X + dx, Y + dy);
    }

    /// <summary>
    /// Returns a debugger-friendly textual representation of the coordinate.
    /// </summary>
    /// <returns>A formatted world-tile coordinate string.</returns>
    public override string ToString()
    {
        return $"WorldTileCoord({X}, {Y})";
    }
}
