using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.World.Objects;

namespace TileWorld.Engine.Runtime.Contexts;

/// <summary>
/// Supplies metadata and behavioral flags for object placement operations.
/// </summary>
public sealed class ObjectPlacementContext
{
    /// <summary>
    /// Gets the actor identifier responsible for the placement request.
    /// </summary>
    public int ActorEntityId { get; init; }

    /// <summary>
    /// Gets the logical source of the placement request.
    /// </summary>
    public PlacementSource Source { get; init; } = PlacementSource.Unknown;

    /// <summary>
    /// Gets a value indicating whether placement validation should be bypassed.
    /// </summary>
    public bool IgnoreValidation { get; init; }

    /// <summary>
    /// Gets a value indicating whether placement events should be suppressed.
    /// </summary>
    public bool SuppressEvents { get; init; }

    /// <summary>
    /// Gets a value indicating whether object drops should be suppressed when the placed object is later destroyed.
    /// </summary>
    public bool SuppressDrops { get; init; }

    /// <summary>
    /// Gets the requested facing direction of the placed object.
    /// </summary>
    public Direction Direction { get; init; }
}
