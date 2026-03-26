using TileWorld.Engine.Render;

namespace TileWorld.Engine.Content.Items;

/// <summary>
/// Describes an item definition used by the prototype drop pipeline.
/// </summary>
public sealed class ItemDef
{
    /// <summary>
    /// Gets the stable numeric identifier used to reference this item in gameplay and persistence flows.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the human-readable item name used by tooling and debug output.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the maximum stack size intended for higher-level inventory systems.
    /// </summary>
    public int MaxStack { get; init; } = 999;

    /// <summary>
    /// Gets the visual metadata used by render backends to draw this item when represented as a world drop.
    /// </summary>
    public TileVisualDef Visual { get; init; } = TileVisualDef.Default;
}
