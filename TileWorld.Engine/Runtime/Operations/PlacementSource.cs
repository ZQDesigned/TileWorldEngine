namespace TileWorld.Engine.Runtime.Operations;

/// <summary>
/// Identifies the origin of a tile placement operation.
/// </summary>
public enum PlacementSource
{
    Unknown = 0,
    Player = 1,
    WorldGeneration = 2,
    DebugTool = 3,
    NetworkSync = 4,
    System = 5
}
