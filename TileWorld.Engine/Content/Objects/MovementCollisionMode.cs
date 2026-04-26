namespace TileWorld.Engine.Content.Objects;

/// <summary>
/// Describes how a placed object contributes to movement collision checks.
/// </summary>
public enum MovementCollisionMode
{
    /// <summary>
    /// The object does not block movement.
    /// </summary>
    None = 0,

    /// <summary>
    /// The object blocks movement over its blocking footprint tiles.
    /// </summary>
    Solid = 1,

    /// <summary>
    /// Reserved one-way platform behavior. Current runtime treats this mode as a blocking footprint.
    /// </summary>
    TopOnly = 2
}
