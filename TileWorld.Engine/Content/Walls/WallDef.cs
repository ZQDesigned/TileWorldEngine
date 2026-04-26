using TileWorld.Engine.Render;

namespace TileWorld.Engine.Content.Walls;

/// <summary>
/// Describes a background wall type that can appear in the world.
/// </summary>
public sealed class WallDef
{
    /// <summary>
    /// Gets the stable numeric identifier used to reference this wall in world cell data.
    /// </summary>
    public ushort Id { get; init; }

    /// <summary>
    /// Gets the human-readable wall name used by tools and debug output.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the autotile connectivity group identifier. A value of <c>0</c> disables autotiling semantics.
    /// </summary>
    public ushort AutoTileGroupId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the wall should count as an enclosed-room wall in higher-level systems.
    /// </summary>
    public bool CountsAsRoomWall { get; init; }

    /// <summary>
    /// Gets a value indicating whether the wall visually obscures far-background decoration.
    /// </summary>
    public bool ObscuresBackground { get; init; }

    /// <summary>
    /// Gets the visual metadata used by render backends to draw this wall.
    /// </summary>
    public TileVisualDef Visual { get; init; } = TileVisualDef.Default;

    /// <summary>
    /// Gets the item definition identifier dropped when this wall is removed through gameplay semantics.
    /// </summary>
    public int BreakDropItemId { get; init; }
}
