namespace TileWorld.Engine.World.Objects;

/// <summary>
/// Represents a simple facing direction for world object instances.
/// </summary>
public enum Direction : byte
{
    /// <summary>
    /// Uses the definition default orientation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Faces left.
    /// </summary>
    Left = 1,

    /// <summary>
    /// Faces right.
    /// </summary>
    Right = 2
}
