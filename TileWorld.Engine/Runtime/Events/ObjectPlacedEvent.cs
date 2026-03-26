using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Objects;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after an object instance is successfully registered in the world.
/// </summary>
/// <param name="ObjectInstanceId">The created object instance identifier.</param>
/// <param name="ObjectDefId">The placed object definition identifier.</param>
/// <param name="AnchorCoord">The logical anchor coordinate of the object.</param>
/// <param name="Direction">The facing direction of the placed object.</param>
/// <param name="ActorEntityId">The entity identifier responsible for the placement.</param>
public readonly record struct ObjectPlacedEvent(
    int ObjectInstanceId,
    int ObjectDefId,
    WorldTileCoord AnchorCoord,
    Direction Direction,
    int ActorEntityId);
