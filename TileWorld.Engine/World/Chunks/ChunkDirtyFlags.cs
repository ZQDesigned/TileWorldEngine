using System;

namespace TileWorld.Engine.World.Chunks;

/// <summary>
/// Describes which cached subsystems need to refresh for a chunk.
/// </summary>
[Flags]
public enum ChunkDirtyFlags
{
    None = 0,
    RenderDirty = 1 << 0,
    CollisionDirty = 1 << 1,
    LightDirty = 1 << 2,
    LiquidDirty = 1 << 3,
    SaveDirty = 1 << 4,
    AutoTileDirty = 1 << 5
}
