namespace TileWorld.Engine.World.Chunks;

/// <summary>
/// Describes the current lifetime state of a chunk.
/// </summary>
public enum ChunkState
{
    Unloaded = 0,
    Loading = 1,
    Loaded = 2,
    Active = 3,
    Inactive = 4,
    Saving = 5
}
