using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a chunk becomes active.
/// </summary>
/// <param name="Coord">The activated chunk coordinate.</param>
public readonly record struct ChunkActivatedEvent(ChunkCoord Coord);
