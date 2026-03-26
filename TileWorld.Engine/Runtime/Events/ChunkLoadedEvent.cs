using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a chunk enters memory.
/// </summary>
/// <param name="Coord">The loaded chunk coordinate.</param>
/// <param name="LoadedFromDisk">Whether the chunk payload came from persistent storage.</param>
/// <param name="CreatedNew">Whether the chunk was created as a fresh empty chunk.</param>
public readonly record struct ChunkLoadedEvent(ChunkCoord Coord, bool LoadedFromDisk, bool CreatedNew);
