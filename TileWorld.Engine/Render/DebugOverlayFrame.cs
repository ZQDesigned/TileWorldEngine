using System.Collections.Generic;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Render;

/// <summary>
/// Captures the output of a debug overlay build pass.
/// </summary>
public sealed class DebugOverlayFrame
{
    /// <summary>
    /// Creates a debug overlay frame.
    /// </summary>
    /// <param name="hoveredTileCoord">The hovered tile coordinate when the cursor is inside the viewport.</param>
    /// <param name="hoveredChunkCoord">The chunk coordinate containing the hovered tile.</param>
    /// <param name="hoveredLocalCoord">The hovered tile coordinate in local chunk space.</param>
    /// <param name="panelLines">The textual lines shown in the debug panel.</param>
    /// <param name="drawCommands">The draw commands generated for the overlay.</param>
    public DebugOverlayFrame(
        WorldTileCoord? hoveredTileCoord,
        ChunkCoord? hoveredChunkCoord,
        Int2? hoveredLocalCoord,
        IReadOnlyList<string> panelLines,
        IReadOnlyList<SpriteDrawCommand> drawCommands)
    {
        HoveredTileCoord = hoveredTileCoord;
        HoveredChunkCoord = hoveredChunkCoord;
        HoveredLocalCoord = hoveredLocalCoord;
        PanelLines = panelLines;
        DrawCommands = drawCommands;
    }

    /// <summary>
    /// Gets the hovered world-tile coordinate when the cursor is inside the viewport.
    /// </summary>
    public WorldTileCoord? HoveredTileCoord { get; }

    /// <summary>
    /// Gets the chunk coordinate containing the hovered tile.
    /// </summary>
    public ChunkCoord? HoveredChunkCoord { get; }

    /// <summary>
    /// Gets the hovered tile coordinate in local chunk space.
    /// </summary>
    public Int2? HoveredLocalCoord { get; }

    /// <summary>
    /// Gets the textual lines displayed by the debug panel.
    /// </summary>
    public IReadOnlyList<string> PanelLines { get; }

    /// <summary>
    /// Gets the overlay draw commands generated for the current frame.
    /// </summary>
    public IReadOnlyList<SpriteDrawCommand> DrawCommands { get; }
}
