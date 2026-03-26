namespace TileWorld.Engine.World;

/// <summary>
/// Describes how world bounds should be interpreted by higher-level systems.
/// </summary>
public enum WorldBoundsMode
{
    /// <summary>
    /// The world has large but finite bounds enforced by higher-level systems.
    /// </summary>
    LargeFinite = 0,
    /// <summary>
    /// The world is bounded in one direction but may extend indefinitely in another.
    /// </summary>
    SemiInfinite = 1,
    /// <summary>
    /// The world is treated as unbounded.
    /// </summary>
    Infinite = 2
}
