using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after an object instance is removed from the world.
/// </summary>
/// <param name="ObjectInstanceId">The removed object instance identifier.</param>
/// <param name="ObjectDefId">The removed object definition identifier.</param>
/// <param name="AnchorCoord">The logical anchor coordinate of the removed object.</param>
/// <param name="Destroyed">Whether the object was destroyed rather than silently unloaded.</param>
public readonly record struct ObjectRemovedEvent(
    int ObjectInstanceId,
    int ObjectDefId,
    WorldTileCoord AnchorCoord,
    bool Destroyed);
