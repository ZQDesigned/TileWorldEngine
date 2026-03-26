using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Runtime.Entities;

/// <summary>
/// Represents a prototype runtime entity managed by the engine.
/// </summary>
public sealed class Entity
{
    /// <summary>
    /// Gets the stable runtime identifier of this entity.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the prototype entity type.
    /// </summary>
    public required EntityType Type { get; init; }

    /// <summary>
    /// Gets or sets the entity position in world tile units.
    /// </summary>
    public Float2 Position { get; set; } = Float2.Zero;

    /// <summary>
    /// Gets or sets the entity velocity in tile units per second.
    /// </summary>
    public Float2 Velocity { get; set; } = Float2.Zero;

    /// <summary>
    /// Gets or sets the entity-local collision bounds expressed in tile units.
    /// </summary>
    public AabbF LocalBounds { get; set; } = new(0f, 0f, 1f, 1f);

    /// <summary>
    /// Gets or sets the entity runtime state flags.
    /// </summary>
    public EntityStateFlags StateFlags { get; set; }

    /// <summary>
    /// Gets or sets the item definition identifier associated with this entity when it represents a world drop.
    /// </summary>
    public int ItemDefId { get; set; }

    /// <summary>
    /// Gets or sets the carried amount associated with this entity when it represents a world drop.
    /// </summary>
    public int Amount { get; set; } = 1;

    /// <summary>
    /// Gets the entity bounds in world tile units.
    /// </summary>
    public AabbF WorldBounds => LocalBounds.Translate(Position);
}
