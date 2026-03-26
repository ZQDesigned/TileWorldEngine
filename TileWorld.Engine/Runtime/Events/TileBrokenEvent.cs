using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a tile is broken through the semantic editing API.
/// </summary>
/// <param name="Coord">The broken world-tile coordinate.</param>
/// <param name="PreviousTileId">The foreground tile identifier that was removed.</param>
/// <param name="Source">The logical source of the break request.</param>
/// <param name="ActorEntityId">The actor responsible for the break request.</param>
public readonly record struct TileBrokenEvent(
    WorldTileCoord Coord,
    ushort PreviousTileId,
    BreakSource Source,
    int ActorEntityId);
