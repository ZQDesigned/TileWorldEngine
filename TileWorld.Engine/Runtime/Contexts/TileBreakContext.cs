using TileWorld.Engine.Runtime.Operations;

namespace TileWorld.Engine.Runtime.Contexts;

/// <summary>
/// Supplies metadata and behavioral flags for tile break operations.
/// </summary>
public sealed class TileBreakContext
{
    /// <summary>
    /// Gets the actor identifier responsible for the break request.
    /// </summary>
    public int ActorEntityId { get; init; }

    /// <summary>
    /// Gets the logical source of the break request.
    /// </summary>
    public BreakSource Source { get; init; } = BreakSource.Unknown;

    /// <summary>
    /// Gets a value indicating whether mining hardness rules should be bypassed.
    /// </summary>
    public bool IgnoreHardness { get; init; }

    /// <summary>
    /// Gets a value reserved for future drop-spawn behavior.
    /// </summary>
    public bool SpawnDrops { get; init; }

    /// <summary>
    /// Gets a value indicating whether break events should be suppressed.
    /// </summary>
    public bool SuppressEvents { get; init; }
}
