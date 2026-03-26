using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a chunk coordinate is queued for background prefetch.
/// </summary>
/// <param name="Coord">The chunk coordinate that was queued.</param>
public readonly record struct ChunkQueuedEvent(ChunkCoord Coord);
