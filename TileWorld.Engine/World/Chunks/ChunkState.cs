namespace TileWorld.Engine.World.Chunks;

/// <summary>
/// Describes the current lifetime state of a chunk.
/// </summary>
public enum ChunkState
{
    Unloaded = 0,
    Queued = 1,
    Loading = 2,
    Loaded = 3,
    Active = 4,
    Inactive = 5,
    Saving = 6
}
