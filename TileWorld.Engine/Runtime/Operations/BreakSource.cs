namespace TileWorld.Engine.Runtime.Operations;

/// <summary>
/// Identifies the origin of a tile break operation.
/// </summary>
public enum BreakSource
{
    Unknown = 0,
    Player = 1,
    Explosion = 2,
    WorldReset = 3,
    DebugTool = 4,
    System = 5
}
