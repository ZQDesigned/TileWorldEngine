using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a world drop entity is spawned.
/// </summary>
/// <param name="EntityId">The spawned drop entity identifier.</param>
/// <param name="ItemDefId">The dropped item definition identifier.</param>
/// <param name="Position">The spawned world position in tile units.</param>
/// <param name="Amount">The dropped item amount.</param>
public readonly record struct DropSpawnedEvent(int EntityId, int ItemDefId, Float2 Position, int Amount);
