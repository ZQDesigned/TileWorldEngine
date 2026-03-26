using TileWorld.Engine.World.Chunks;

namespace TileWorld.Engine.Runtime.Chunks;

/// <summary>
/// Describes how a chunk became available to the runtime.
/// </summary>
/// <param name="Chunk">The resolved chunk instance.</param>
/// <param name="WasLoadedFromMemory">Whether the chunk was already available in memory.</param>
/// <param name="WasLoadedFromDisk">Whether the chunk payload was loaded from persistent storage.</param>
/// <param name="WasCreatedNew">Whether the chunk had to be created as a new empty chunk.</param>
public readonly record struct ChunkLoadResult(
    Chunk Chunk,
    bool WasLoadedFromMemory,
    bool WasLoadedFromDisk,
    bool WasCreatedNew);
