namespace TileWorld.Engine.Runtime.Chunks;

/// <summary>
/// Describes where a chunk payload was resolved from.
/// </summary>
public enum ChunkLoadSource
{
    /// <summary>
    /// The chunk was already available in memory.
    /// </summary>
    Memory = 0,

    /// <summary>
    /// The chunk payload was loaded from persistent storage.
    /// </summary>
    Disk = 1,

    /// <summary>
    /// The chunk payload was generated procedurally.
    /// </summary>
    Generated = 2,

    /// <summary>
    /// The chunk was created as an empty fallback because no storage or generator data was available.
    /// </summary>
    EmptyCreated = 3
}
