using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Render;

namespace TileWorld.Engine.Content.Objects;

/// <summary>
/// Describes a placeable multi-tile world object such as furniture or stations.
/// </summary>
public sealed class ObjectDef
{
    /// <summary>
    /// Gets the stable numeric identifier used to reference this object definition.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the human-readable object name used by tools and debug output.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the object footprint size in tiles.
    /// </summary>
    public Int2 SizeInTiles { get; init; } = Int2.One;

    /// <summary>
    /// Gets the offset from the top-left footprint origin to the logical anchor coordinate used by placement APIs.
    /// </summary>
    public Int2 AnchorOffset { get; init; } = Int2.Zero;

    /// <summary>
    /// Gets a value indicating whether the object participates in interaction workflows.
    /// </summary>
    public bool IsInteractive { get; init; }

    /// <summary>
    /// Gets a value indicating whether the object requires solid support below its bottom edge.
    /// </summary>
    public bool RequiresSupport { get; init; }

    /// <summary>
    /// Gets the item definition identifier spawned when the object is destroyed. A value of <c>0</c> disables drops.
    /// </summary>
    public int BreakDropItemId { get; init; }

    /// <summary>
    /// Gets the visual metadata used by render backends to draw this object.
    /// </summary>
    public TileVisualDef Visual { get; init; } = TileVisualDef.Default;
}
