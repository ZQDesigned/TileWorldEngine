using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Input;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Render;

/// <summary>
/// Produces the debug overlay used by the desktop test application.
/// </summary>
/// <remarks>
/// This API is intended for engine infrastructure and advanced tooling. Prefer <see cref="Runtime.WorldRuntime"/>
/// as the stable gameplay-facing entry point.
/// </remarks>
public sealed class DebugOverlayRenderer
{
    private const float ChunkDirtyMarkerDepth = 0.8f;
    private const float ChunkBoundaryDepth = 0.82f;
    private const float HoveredTileDepth = 0.86f;
    private const float PanelBackgroundDepth = 0.9f;
    private const float PanelTextDepth = 0.95f;
    private static readonly RectI WhitePixelSourceRect = new(0, 0, 1, 1);
    private static readonly ColorRgba32 ChunkBoundaryColor = new(255, 255, 255, 160);
    private static readonly ColorRgba32 DirtyChunkOutlineColor = new(255, 190, 40, 220);
    private static readonly ColorRgba32 DirtyChunkCornerColor = new(255, 190, 40, 180);
    private static readonly ColorRgba32 HoveredTileFillColor = new(0, 220, 255, 72);
    private static readonly ColorRgba32 PanelBackgroundColor = new(0, 0, 0, 190);
    private static readonly ColorRgba32 PanelTextColor = ColorRgba32.White;
    private readonly DebugBitmapFont5x7 _font;
    private readonly WorldRenderSettings _settings;

    /// <summary>
    /// Creates a debug overlay renderer.
    /// </summary>
    /// <param name="settings">The render settings that define tile and chunk pixel sizes.</param>
    /// <param name="font">The debug font used for panel text.</param>
    /// <param name="textureKey">The white-pixel texture key used to draw overlay primitives.</param>
    public DebugOverlayRenderer(WorldRenderSettings settings, DebugBitmapFont5x7 font = null, string textureKey = "debug/white")
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(textureKey);

        _settings = settings;
        _font = font ?? new DebugBitmapFont5x7();
        TextureKey = textureKey;
    }

    public string TextureKey { get; }

    /// <summary>
    /// Builds a frame of overlay draw commands and textual panel content.
    /// </summary>
    /// <param name="runtime">The world runtime being inspected.</param>
    /// <param name="worldRenderer">The renderer that defines the visible chunk set.</param>
    /// <param name="camera">The active camera.</param>
    /// <param name="input">The current frame input snapshot.</param>
    /// <param name="selectedTileId">The tile currently selected by the debug application.</param>
    /// <param name="selectionLabel">An optional label that should override the default selection text.</param>
    /// <returns>A frame containing textual lines and generated overlay draw commands.</returns>
    public DebugOverlayFrame Build(
        WorldRuntime runtime,
        WorldRenderer worldRenderer,
        Camera2D camera,
        FrameInput input,
        ushort selectedTileId,
        string selectionLabel = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(worldRenderer);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(input);

        var commands = new List<SpriteDrawCommand>();
        var hoveredTileCoord = TryGetHoveredTileCoord(camera, input);
        var hoveredChunkCoord = default(ChunkCoord?);
        var hoveredLocalCoord = default(Int2?);
        var hoveredCell = TileCell.Empty;
        var hoveredBiomeId = 0;
        var hoveredBiomeName = string.Empty;
        var hoveredDirtyFlags = ChunkDirtyFlags.None;
        var hoveredChunkLoaded = false;
        var hoveredLightLevel = (byte)0;
        var hoveredMovementBlocker = MovementBlockerKind.None;
        var hoveredObjectLine = string.Empty;
        var hasPlayer = false;
        var playerVelocity = Float2.Zero;
        var playerInLiquid = false;
        var playerSubmersion = 0f;
        var playerLiquidKind = LiquidKind.None;

        var primaryPlayer = runtime
            .EnumerateEntities()
            .Where(static entity => entity.Type == EntityType.Player)
            .OrderBy(static entity => entity.EntityId)
            .FirstOrDefault();
        if (primaryPlayer is not null)
        {
            hasPlayer = true;
            playerVelocity = primaryPlayer.Velocity;
            playerInLiquid = primaryPlayer.IsInLiquid;
            playerSubmersion = primaryPlayer.Submersion;
            playerLiquidKind = primaryPlayer.CurrentLiquidType;
        }

        AddVisibleChunkHighlights(commands, runtime, worldRenderer, camera);

        if (hoveredTileCoord is { } tileCoord)
        {
            hoveredChunkCoord = WorldCoordinateConverter.ToChunkCoord(tileCoord);
            hoveredLocalCoord = WorldCoordinateConverter.ToLocalCoord(tileCoord);
            hoveredCell = runtime.QueryService.GetCell(tileCoord);
            hoveredBiomeId = runtime.GetBiomeId(tileCoord);
            hoveredBiomeName = runtime.TryGetBiomeDef(tileCoord, out var hoveredBiomeDef)
                ? hoveredBiomeDef.Name.ToUpperInvariant()
                : "UNKNOWN";
            hoveredLightLevel = runtime.GetLightLevel(tileCoord);
            hoveredMovementBlocker = runtime.QueryService.GetMovementBlocker(tileCoord, EntityType.Player);
            hoveredChunkLoaded = runtime.WorldData.TryGetChunk(hoveredChunkCoord.Value, out var hoveredChunk);
            hoveredDirtyFlags = hoveredChunkLoaded ? hoveredChunk.DirtyFlags : ChunkDirtyFlags.None;
            if (runtime.QueryService.TryGetObjectAt(tileCoord, out var hoveredObject) &&
                runtime.ContentRegistry.TryGetObjectDef(hoveredObject.ObjectDefId, out var hoveredObjectDef))
            {
                hoveredObjectLine = $"OBJECT: {hoveredObject.InstanceId.ToString(CultureInfo.InvariantCulture)} {hoveredObjectDef.Name.ToUpperInvariant()}";
            }

            AddHoveredTileHighlight(commands, camera, tileCoord);
        }

        var panelLines = BuildPanelLines(
            runtime,
            selectedTileId,
            selectionLabel,
            camera,
            hoveredTileCoord,
            hoveredChunkCoord,
            hoveredLocalCoord,
            hoveredCell,
            hoveredBiomeId,
            hoveredBiomeName,
            hoveredDirtyFlags,
            hoveredChunkLoaded,
            hoveredLightLevel,
            hoveredMovementBlocker,
            hoveredObjectLine,
            hasPlayer,
            playerVelocity,
            playerInLiquid,
            playerSubmersion,
            playerLiquidKind);

        AddPanel(commands, panelLines);

        return new DebugOverlayFrame(hoveredTileCoord, hoveredChunkCoord, hoveredLocalCoord, panelLines, commands);
    }

    /// <summary>
    /// Draws the debug overlay directly into a render context.
    /// </summary>
    /// <param name="runtime">The world runtime being inspected.</param>
    /// <param name="worldRenderer">The renderer that defines the visible chunk set.</param>
    /// <param name="camera">The active camera.</param>
    /// <param name="input">The current frame input snapshot.</param>
    /// <param name="renderContext">The render context that receives overlay draw commands.</param>
    /// <param name="selectedTileId">The tile currently selected by the debug application.</param>
    /// <param name="selectionLabel">An optional label that should override the default selection text.</param>
    public void Draw(
        WorldRuntime runtime,
        WorldRenderer worldRenderer,
        Camera2D camera,
        FrameInput input,
        IRenderContext renderContext,
        ushort selectedTileId,
        string selectionLabel = null)
    {
        ArgumentNullException.ThrowIfNull(renderContext);

        var frame = Build(runtime, worldRenderer, camera, input, selectedTileId, selectionLabel);
        foreach (var command in frame.DrawCommands)
        {
            renderContext.DrawSprite(command);
        }
    }

    /// <summary>
    /// Attempts to resolve the world-tile coordinate currently under the cursor.
    /// </summary>
    /// <param name="camera">The camera used to transform screen-space input into world space.</param>
    /// <param name="input">The current frame input snapshot.</param>
    /// <returns>The hovered world-tile coordinate when the cursor is inside the viewport; otherwise <see langword="null"/>.</returns>
    public WorldTileCoord? TryGetHoveredTileCoord(Camera2D camera, FrameInput input)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(input);

        if (!input.IsMouseInsideViewport)
        {
            return null;
        }

        var worldPixel = camera.ScreenToWorldPixels(input.MouseScreenPositionPixels);
        return new WorldTileCoord(
            FloorDivide(worldPixel.X, _settings.TileSizePixels),
            FloorDivide(worldPixel.Y, _settings.TileSizePixels));
    }

    private void AddVisibleChunkHighlights(
        List<SpriteDrawCommand> commands,
        WorldRuntime runtime,
        WorldRenderer worldRenderer,
        Camera2D camera)
    {
        foreach (var chunkCoord in worldRenderer.GetVisibleChunkCoords())
        {
            if (!runtime.WorldData.TryGetChunk(chunkCoord, out var chunk))
            {
                continue;
            }

            var chunkBounds = GetChunkScreenBounds(camera, chunkCoord);
            if ((chunk.DirtyFlags & ChunkDirtyFlags.SaveDirty) != ChunkDirtyFlags.None)
            {
                AddDirtyChunkMarker(commands, chunkBounds);
            }

            AddRectOutline(commands, chunkBounds, ChunkBoundaryColor, ChunkBoundaryDepth);
        }
    }

    private void AddHoveredTileHighlight(List<SpriteDrawCommand> commands, Camera2D camera, WorldTileCoord coord)
    {
        var tileScreenBounds = GetTileScreenBounds(camera, coord);
        AddFilledRect(commands, tileScreenBounds, HoveredTileFillColor, HoveredTileDepth);
        AddRectOutline(commands, tileScreenBounds, ChunkBoundaryColor, HoveredTileDepth + 0.01f);
    }

    private IReadOnlyList<string> BuildPanelLines(
        WorldRuntime runtime,
        ushort selectedTileId,
        string selectionLabel,
        Camera2D camera,
        WorldTileCoord? hoveredTileCoord,
        ChunkCoord? hoveredChunkCoord,
        Int2? hoveredLocalCoord,
        TileCell hoveredCell,
        int hoveredBiomeId,
        string hoveredBiomeName,
        ChunkDirtyFlags hoveredDirtyFlags,
        bool hoveredChunkLoaded,
        byte hoveredLightLevel,
        MovementBlockerKind hoveredMovementBlocker,
        string hoveredObjectLine,
        bool hasPlayer,
        Float2 playerVelocity,
        bool playerInLiquid,
        float playerSubmersion,
        LiquidKind playerLiquidKind)
    {
        var effectiveSelectionLabel = !string.IsNullOrWhiteSpace(selectionLabel)
            ? selectionLabel
            : runtime.ContentRegistry.TryGetTileDef(selectedTileId, out var selectedTileDef)
                ? $"{selectedTileId.ToString(CultureInfo.InvariantCulture)} {selectedTileDef.Name.ToUpperInvariant()}"
                : "UNKNOWN";

        var lines = new List<string>
        {
            $"SELECTED: {effectiveSelectionLabel}",
            $"CAMERA: {camera.PositionPixels.X.ToString(CultureInfo.InvariantCulture)},{camera.PositionPixels.Y.ToString(CultureInfo.InvariantCulture)}",
            $"PERSISTENCE: {(runtime.IsPersistenceEnabled ? "ON" : "OFF")}"
        };

        if (hasPlayer)
        {
            lines.Add(
                $"VELOCITY: {playerVelocity.X.ToString("0.00", CultureInfo.InvariantCulture)},{playerVelocity.Y.ToString("0.00", CultureInfo.InvariantCulture)}");
            lines.Add($"IN_LIQUID: {(playerInLiquid ? "YES" : "NO")}");
            lines.Add($"SUBMERSION: {playerSubmersion.ToString("0.00", CultureInfo.InvariantCulture)}");
            lines.Add($"LIQUID_KIND: {FormatLiquidKind(playerLiquidKind)}");
        }

        if (hoveredTileCoord is not { } tileCoord ||
            hoveredChunkCoord is not { } chunkCoord ||
            hoveredLocalCoord is not { } localCoord)
        {
            lines.Add("TILE: OUTSIDE VIEWPORT");
            return lines;
        }

        lines.Add($"TILE: {tileCoord.X.ToString(CultureInfo.InvariantCulture)},{tileCoord.Y.ToString(CultureInfo.InvariantCulture)}");
        lines.Add($"CHUNK: {chunkCoord.X.ToString(CultureInfo.InvariantCulture)},{chunkCoord.Y.ToString(CultureInfo.InvariantCulture)}");
        lines.Add($"LOCAL: {localCoord.X.ToString(CultureInfo.InvariantCulture)},{localCoord.Y.ToString(CultureInfo.InvariantCulture)}");
        lines.Add($"BIOME: {hoveredBiomeId.ToString(CultureInfo.InvariantCulture)} {hoveredBiomeName}");
        lines.Add($"LIGHT: {hoveredLightLevel.ToString(CultureInfo.InvariantCulture)}");
        lines.Add($"BLOCKED BY: {FormatMovementBlocker(hoveredMovementBlocker)}");
        lines.Add($"FG TILE: {hoveredCell.ForegroundTileId.ToString(CultureInfo.InvariantCulture)}");
        lines.Add(
            $"VARIANT: {hoveredCell.Variant.ToString(CultureInfo.InvariantCulture)} FLAGS: {hoveredCell.Flags.ToString(CultureInfo.InvariantCulture)}");
        lines.Add($"BG WALL: {hoveredCell.BackgroundWallId.ToString(CultureInfo.InvariantCulture)}");
        lines.Add(
            $"LIQUID: {hoveredCell.LiquidType.ToString(CultureInfo.InvariantCulture)}/{hoveredCell.LiquidAmount.ToString(CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrWhiteSpace(hoveredObjectLine))
        {
            lines.Add(hoveredObjectLine);
        }
        lines.Add($"DIRTY: {(hoveredChunkLoaded ? FormatDirtyFlags(hoveredDirtyFlags) : "UNLOADED")}");

        return lines;
    }

    private void AddPanel(List<SpriteDrawCommand> commands, IReadOnlyList<string> panelLines)
    {
        const int panelPaddingPixels = 6;
        var panelPosition = new Int2(8, 8);
        var textHeight = panelLines.Count == 0
            ? 0
            : (panelLines.Count * _font.LineHeightPixels) - _font.GlyphSpacingPixels;
        var panelWidth = panelLines.Count == 0
            ? 0
            : panelLines.Max(_font.MeasureTextWidth) + (panelPaddingPixels * 2);
        var panelHeight = textHeight + (panelPaddingPixels * 2);

        if (panelWidth > 0 && panelHeight > 0)
        {
            AddFilledRect(
                commands,
                new RectI(panelPosition.X, panelPosition.Y, panelWidth, panelHeight),
                PanelBackgroundColor,
                PanelBackgroundDepth);
        }

        for (var index = 0; index < panelLines.Count; index++)
        {
            var linePosition = new Int2(
                panelPosition.X + panelPaddingPixels,
                panelPosition.Y + panelPaddingPixels + (index * _font.LineHeightPixels));

            commands.AddRange(_font.CreateDrawCommands(
                panelLines[index],
                linePosition,
                TextureKey,
                PanelTextColor,
                PanelTextDepth));
        }
    }

    private void AddDirtyChunkMarker(List<SpriteDrawCommand> commands, RectI bounds)
    {
        var insetBounds = InsetRect(bounds, 2);
        AddRectOutline(commands, insetBounds, DirtyChunkOutlineColor, ChunkDirtyMarkerDepth, thickness: 2);

        var cornerSize = Math.Min(12, Math.Min(bounds.Width, bounds.Height));
        AddFilledRect(
            commands,
            new RectI(bounds.X + 2, bounds.Y + 2, cornerSize, 4),
            DirtyChunkCornerColor,
            ChunkDirtyMarkerDepth + 0.01f);
        AddFilledRect(
            commands,
            new RectI(bounds.X + 2, bounds.Y + 2, 4, cornerSize),
            DirtyChunkCornerColor,
            ChunkDirtyMarkerDepth + 0.01f);
    }

    private void AddFilledRect(List<SpriteDrawCommand> commands, RectI bounds, ColorRgba32 tint, float layerDepth)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        commands.Add(new SpriteDrawCommand(
            TextureKey,
            WhitePixelSourceRect,
            bounds,
            tint,
            layerDepth));
    }

    private void AddRectOutline(
        List<SpriteDrawCommand> commands,
        RectI bounds,
        ColorRgba32 tint,
        float layerDepth,
        int thickness = 1)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || thickness <= 0)
        {
            return;
        }

        var clampedThickness = Math.Min(thickness, Math.Min(bounds.Width, bounds.Height));
        AddFilledRect(commands, new RectI(bounds.X, bounds.Y, bounds.Width, clampedThickness), tint, layerDepth);
        AddFilledRect(commands, new RectI(bounds.X, bounds.Bottom - clampedThickness, bounds.Width, clampedThickness), tint, layerDepth);
        AddFilledRect(commands, new RectI(bounds.X, bounds.Y, clampedThickness, bounds.Height), tint, layerDepth);
        AddFilledRect(commands, new RectI(bounds.Right - clampedThickness, bounds.Y, clampedThickness, bounds.Height), tint, layerDepth);
    }

    private RectI GetChunkScreenBounds(Camera2D camera, ChunkCoord chunkCoord)
    {
        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(chunkCoord);
        var worldBounds = new RectI(
            chunkOrigin.X * _settings.TileSizePixels,
            chunkOrigin.Y * _settings.TileSizePixels,
            _settings.ChunkWidthPixels,
            _settings.ChunkHeightPixels);

        return WorldToScreenRect(camera, worldBounds);
    }

    private RectI GetTileScreenBounds(Camera2D camera, WorldTileCoord coord)
    {
        var worldBounds = new RectI(
            coord.X * _settings.TileSizePixels,
            coord.Y * _settings.TileSizePixels,
            _settings.TileSizePixels,
            _settings.TileSizePixels);

        return WorldToScreenRect(camera, worldBounds);
    }

    private static RectI WorldToScreenRect(Camera2D camera, RectI worldRect)
    {
        var screenPosition = camera.WorldToScreenPixels(new Int2(worldRect.X, worldRect.Y));
        return new RectI(screenPosition.X, screenPosition.Y, worldRect.Width, worldRect.Height);
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;

        if (remainder < 0)
        {
            quotient--;
        }

        return quotient;
    }

    private static string FormatDirtyFlags(ChunkDirtyFlags dirtyFlags)
    {
        return dirtyFlags == ChunkDirtyFlags.None
            ? "NONE"
            : dirtyFlags.ToString().ToUpperInvariant();
    }

    private static string FormatLiquidKind(LiquidKind liquidKind)
    {
        return liquidKind switch
        {
            LiquidKind.Water => "WATER",
            LiquidKind.Lava => "LAVA",
            LiquidKind.Honey => "HONEY",
            _ => "NONE"
        };
    }

    private static string FormatMovementBlocker(MovementBlockerKind movementBlocker)
    {
        return movementBlocker switch
        {
            MovementBlockerKind.None => "NONE",
            MovementBlockerKind.Tile => "TILE",
            MovementBlockerKind.Object => "OBJECT",
            _ => "UNKNOWN"
        };
    }

    private static RectI InsetRect(RectI rect, int insetPixels)
    {
        var doubleInset = insetPixels * 2;
        var width = Math.Max(1, rect.Width - doubleInset);
        var height = Math.Max(1, rect.Height - doubleInset);
        return new RectI(rect.X + insetPixels, rect.Y + insetPixels, width, height);
    }
}
