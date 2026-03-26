using TileWorld.Engine.World.Chunks;

namespace TileWorld.Engine.World.Generation;

/// <summary>
/// Captures the terrain payload generated for a chunk coordinate.
/// </summary>
/// <param name="Chunk">The generated chunk instance.</param>
public readonly record struct ChunkGenerationResult(Chunk Chunk);
