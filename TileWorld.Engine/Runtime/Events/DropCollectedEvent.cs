namespace TileWorld.Engine.Runtime.Events;

/// <summary>
/// Raised after a player collects a world drop.
/// </summary>
/// <param name="EntityId">The collected drop entity identifier.</param>
/// <param name="ItemDefId">The collected item definition identifier.</param>
/// <param name="CollectorEntityId">The player or actor that collected the drop.</param>
/// <param name="Amount">The collected item amount.</param>
public readonly record struct DropCollectedEvent(int EntityId, int ItemDefId, int CollectorEntityId, int Amount);
