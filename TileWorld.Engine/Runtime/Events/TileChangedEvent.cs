using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a tile foreground identifier changes.
/// </summary>
/// <param name="Coord">The edited world-tile coordinate.</param>
/// <param name="OldTileId">The previous foreground tile identifier.</param>
/// <param name="NewTileId">The new foreground tile identifier.</param>
public readonly record struct TileChangedEvent(WorldTileCoord Coord, ushort OldTileId, ushort NewTileId);
