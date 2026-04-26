namespace TileWorld.Engine.Runtime.Queries;

/// <summary>
/// Describes which layer currently blocks movement at a world-tile coordinate.
/// </summary>
internal enum MovementBlockerKind
{
    None = 0,
    Tile = 1,
    Object = 2
}
