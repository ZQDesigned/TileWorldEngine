using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World.Objects;

/// <summary>
/// Represents a placed object instance stored in the world.
/// </summary>
public sealed class ObjectInstance
{
    /// <summary>
    /// Gets the stable runtime identifier of this object instance.
    /// </summary>
    public required int InstanceId { get; init; }

    /// <summary>
    /// Gets the content definition identifier used to interpret this object instance.
    /// </summary>
    public required int ObjectDefId { get; init; }

    /// <summary>
    /// Gets the logical anchor coordinate used to place this object.
    /// </summary>
    public required WorldTileCoord AnchorCoord { get; init; }

    /// <summary>
    /// Gets the facing direction of this object instance.
    /// </summary>
    public Direction Direction { get; init; }

    /// <summary>
    /// Gets instance state flags reserved for future object-specific runtime state.
    /// </summary>
    public ushort StateFlags { get; init; }
}
