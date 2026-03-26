using System;

namespace TileWorld.Engine.Runtime.Entities;

/// <summary>
/// Stores runtime state flags for an <see cref="Entity"/>.
/// </summary>
[Flags]
public enum EntityStateFlags
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The entity is currently grounded on solid world geometry.
    /// </summary>
    Grounded = 1 << 0,

    /// <summary>
    /// The entity is pending removal.
    /// </summary>
    PendingRemoval = 1 << 1
}
