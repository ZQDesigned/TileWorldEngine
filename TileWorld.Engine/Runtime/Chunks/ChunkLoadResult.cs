using TileWorld.Engine.World.Chunks;

namespace TileWorld.Engine.Runtime.Chunks;

/// <summary>
/// Describes how a chunk became available to the runtime.
/// </summary>
/// <param name="Chunk">The resolved chunk instance.</param>
/// <param name="Source">The source that produced the resolved chunk.</param>
public readonly record struct ChunkLoadResult(
    Chunk Chunk,
    ChunkLoadSource Source);
