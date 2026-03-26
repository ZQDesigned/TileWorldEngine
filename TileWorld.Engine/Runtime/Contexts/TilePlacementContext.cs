using TileWorld.Engine.Runtime.Operations;

namespace TileWorld.Engine.Runtime.Contexts;

/// <summary>
/// Supplies metadata and behavioral flags for tile placement operations.
/// </summary>
public sealed class TilePlacementContext
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
    /// Gets a value reserved for future drop-suppression behavior.
    /// </summary>
    public bool SuppressDrops { get; init; }

    /// <summary>
    /// Gets an optional explicit autotile variant to seed before post-processing runs.
    /// </summary>
    public ushort? VariantHint { get; init; }
}
