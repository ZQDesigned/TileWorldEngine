using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised immediately before a chunk is removed from memory.
/// </summary>
/// <param name="Coord">The unloading chunk coordinate.</param>
public readonly record struct ChunkUnloadingEvent(ChunkCoord Coord);
