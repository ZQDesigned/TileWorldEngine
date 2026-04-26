using System;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Input;
using TileWorld.Engine.Render;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Render;

public sealed class DebugOverlayRendererTests
{
    [Fact]
    public void Build_WhenMouseInsideViewportIncludesHoveredTileAndDirtyChunkHighlights()
    {
        var runtime = CreateRuntime();
        var settings = new WorldRenderSettings();
        var camera = new Camera2D(Int2.Zero, new Int2(512, 512));
        var renderer = new WorldRenderer(camera, new ChunkRenderCacheBuilder(runtime.ContentRegistry, settings), settings);
        var overlay = new DebugOverlayRenderer(settings);

        runtime.Initialize();
        runtime.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        var frame = overlay.Build(
            runtime,
            renderer,
            camera,
            new FrameInput(
                new Int2(8, 8),
                isMouseInsideViewport: true,
                leftButton: default,
                middleButton: default,
                rightButton: default,
                mouseWheelDelta: 0),
            selectedTileId: 1);

        Assert.Equal(new WorldTileCoord(0, 0), frame.HoveredTileCoord);
        Assert.Equal(new ChunkCoord(0, 0), frame.HoveredChunkCoord);
        Assert.Equal(new Int2(0, 0), frame.HoveredLocalCoord);
        Assert.Contains("TILE: 0,0", frame.PanelLines);
        Assert.Contains(frame.PanelLines, line => line.StartsWith("LIGHT: ", StringComparison.Ordinal));
        Assert.Contains(frame.DrawCommands, command => command.LayerDepth == 0.81f && command.DestinationRectPixels == new RectI(2, 2, 12, 4));
        Assert.Contains(frame.DrawCommands, command => command.LayerDepth == 0.86f && command.DestinationRectPixels == new RectI(0, 0, 16, 16));
    }

    [Fact]
    public void Build_WhenMouseIsOutsideViewportOmitsHoveredTileInfo()
    {
        var runtime = CreateRuntime();
        var settings = new WorldRenderSettings();
        var camera = new Camera2D(new Int2(-16, -16), new Int2(512, 512));
        var renderer = new WorldRenderer(camera, new ChunkRenderCacheBuilder(runtime.ContentRegistry, settings), settings);
        var overlay = new DebugOverlayRenderer(settings);

        runtime.Initialize();

        var frame = overlay.Build(
            runtime,
            renderer,
            camera,
            FrameInput.Empty,
            selectedTileId: 1);

        Assert.Null(frame.HoveredTileCoord);
        Assert.Contains("TILE: OUTSIDE VIEWPORT", frame.PanelLines);
    }

    [Fact]
    public void Build_DoesNotHighlightChunkWhenOnlyNonSaveDirtyFlagsRemain()
    {
        var runtime = CreateRuntime();
        var settings = new WorldRenderSettings();
        var camera = new Camera2D(Int2.Zero, new Int2(512, 512));
        var renderer = new WorldRenderer(camera, new ChunkRenderCacheBuilder(runtime.ContentRegistry, settings), settings);
        var overlay = new DebugOverlayRenderer(settings);

        runtime.Initialize();
        var chunk = runtime.WorldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        chunk.DirtyFlags = ChunkDirtyFlags.AutoTileDirty | ChunkDirtyFlags.CollisionDirty;

        var frame = overlay.Build(
            runtime,
            renderer,
            camera,
            new FrameInput(
                new Int2(8, 8),
                isMouseInsideViewport: true,
                leftButton: default,
                middleButton: default,
                rightButton: default,
                mouseWheelDelta: 0),
            selectedTileId: 1);

        Assert.DoesNotContain(frame.DrawCommands, command => command.LayerDepth == 0.81f);
        var dirtyLine = Assert.Single(frame.PanelLines, line => line.StartsWith("DIRTY: ", StringComparison.Ordinal));
        Assert.Contains("AUTOTILEDIRTY", dirtyLine, StringComparison.Ordinal);
        Assert.Contains("COLLISIONDIRTY", dirtyLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WhenPlayerExists_IncludesPlayerLiquidStateLines()
    {
        var runtime = CreateRuntime();
        var settings = new WorldRenderSettings();
        var camera = new Camera2D(Int2.Zero, new Int2(512, 512));
        var renderer = new WorldRenderer(camera, new ChunkRenderCacheBuilder(runtime.ContentRegistry, settings), settings);
        var overlay = new DebugOverlayRenderer(settings);

        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));
        var playerId = runtime.SpawnPlayer(new Float2(5.5f, 5f));
        runtime.SetLiquid(new WorldTileCoord(5, 5), (byte)LiquidKind.Water, 255);
        runtime.SetLiquid(new WorldTileCoord(6, 5), (byte)LiquidKind.Water, 255);
        runtime.SetLiquid(new WorldTileCoord(5, 6), (byte)LiquidKind.Water, 255);
        runtime.SetLiquid(new WorldTileCoord(6, 6), (byte)LiquidKind.Water, 255);
        runtime.Update(new FrameTime(TimeSpan.FromSeconds(1d / 60d), TimeSpan.FromSeconds(1d / 60d), false));
        Assert.True(runtime.TryGetEntity(playerId, out var player));
        Assert.Equal(EntityType.Player, player.Type);

        var frame = overlay.Build(
            runtime,
            renderer,
            camera,
            FrameInput.Empty,
            selectedTileId: 1);

        Assert.Contains(frame.PanelLines, line => line.StartsWith("VELOCITY: ", StringComparison.Ordinal));
        Assert.Contains("IN_LIQUID: YES", frame.PanelLines);
        Assert.Contains(frame.PanelLines, line => line.StartsWith("SUBMERSION: ", StringComparison.Ordinal));
        Assert.Contains("LIQUID_KIND: WATER", frame.PanelLines);
    }

    private static WorldRuntime CreateRuntime()
    {
        var registry = new ContentRegistry();
        registry.RegisterTile(new TileDef
        {
            Id = 1,
            Name = "Stone",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1,
            AutoTileGroupId = 1,
            Visual = new TileVisualDef(
                "debug/white",
                new RectI(0, 0, 1, 1),
                new ColorRgba32(120, 120, 120),
                false)
        });

        return new WorldRuntime(new WorldData(new WorldMetadata()), registry);
    }
}
