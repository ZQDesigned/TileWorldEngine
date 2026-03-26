namespace TileWorld.Engine.Runtime.Entities;

/// <summary>
/// Identifies the prototype entity category represented by an <see cref="Entity"/>.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// A controllable player prototype.
    /// </summary>
    Player = 0,

    /// <summary>
    /// A collectible world drop.
    /// </summary>
    Drop = 1
}
