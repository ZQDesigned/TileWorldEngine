using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a tile is placed through the semantic editing API.
/// </summary>
/// <param name="Coord">The placed world-tile coordinate.</param>
/// <param name="TileId">The foreground tile identifier that was placed.</param>
/// <param name="Source">The logical source of the placement.</param>
/// <param name="ActorEntityId">The actor responsible for the placement.</param>
public readonly record struct TilePlacedEvent(
    WorldTileCoord Coord,
    ushort TileId,
    PlacementSource Source,
    int ActorEntityId);
