using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Runtime.Entities;

/// <summary>
/// Describes the initial state for a spawned entity.
/// </summary>
public sealed class EntitySpawnRequest
{
    /// <summary>
    /// Gets the prototype entity type to spawn.
    /// </summary>
    public EntityType Type { get; init; }

    /// <summary>
    /// Gets the initial position in world tile units.
    /// </summary>
    public Float2 Position { get; init; } = Float2.Zero;

    /// <summary>
    /// Gets the entity-local collision bounds expressed in tile units.
    /// </summary>
    public AabbF LocalBounds { get; init; } = new(0f, 0f, 1f, 1f);

    /// <summary>
    /// Gets the initial velocity in tile units per second.
    /// </summary>
    public Float2 Velocity { get; init; } = Float2.Zero;

    /// <summary>
    /// Gets the item definition identifier associated with this entity when it represents a world drop.
    /// </summary>
    public int ItemDefId { get; init; }

    /// <summary>
    /// Gets the carried amount associated with this entity when it represents a world drop.
    /// </summary>
    public int Amount { get; init; } = 1;
}
