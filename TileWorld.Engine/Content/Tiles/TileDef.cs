using TileWorld.Engine.Render;

namespace TileWorld.Engine.Content.Tiles;

/// <summary>
/// Describes a tile type that can appear in the world.
/// </summary>
public sealed class TileDef
{
    /// <summary>
    /// Gets the stable numeric identifier used to reference this tile in world cell data.
    /// </summary>
    public ushort Id { get; init; }

    /// <summary>
    /// Gets the human-readable tile name used by tools and debug output.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the loose content category used to group tiles in tooling and authoring flows.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the tile should be treated as solid for collision and support checks.
    /// </summary>
    public bool IsSolid { get; init; }

    /// <summary>
    /// Gets a value indicating whether the tile blocks light propagation.
    /// </summary>
    public bool BlocksLight { get; init; }

    /// <summary>
    /// Gets a value indicating whether the tile can be broken by ordinary mining operations.
    /// </summary>
    public bool CanBeMined { get; init; }

    /// <summary>
    /// Gets the relative mining hardness used by higher-level gameplay systems.
    /// </summary>
    public int Hardness { get; init; }

    /// <summary>
    /// Gets the single-channel emissive light level emitted by this tile in the range <c>0..15</c>.
    /// </summary>
    public byte EmissiveLight { get; init; }

    /// <summary>
    /// Gets the item definition identifier spawned when the tile is broken. A value of <c>0</c> disables drops.
    /// </summary>
    public int BreakDropItemId { get; init; }

    /// <summary>
    /// Gets the autotile connectivity group identifier. A value of <c>0</c> disables autotiling.
    /// </summary>
    public ushort AutoTileGroupId { get; init; }

    /// <summary>
    /// Gets the visual metadata used by render backends to draw this tile.
    /// </summary>
    public TileVisualDef Visual { get; init; } = TileVisualDef.Default;
}
