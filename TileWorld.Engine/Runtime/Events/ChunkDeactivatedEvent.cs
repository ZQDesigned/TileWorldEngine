using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a chunk leaves the active set but before it is unloaded from memory.
/// </summary>
/// <param name="Coord">The deactivated chunk coordinate.</param>
public readonly record struct ChunkDeactivatedEvent(ChunkCoord Coord);
