using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a chunk enters memory.
/// </summary>
/// <param name="Coord">The loaded chunk coordinate.</param>
/// <param name="Source">The source that produced the loaded chunk.</param>
public readonly record struct ChunkLoadedEvent(ChunkCoord Coord, ChunkLoadSource Source);
