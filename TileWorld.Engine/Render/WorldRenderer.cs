using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.Lighting;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Objects;

namespace TileWorld.Engine.Render;

/// <summary>
/// Maintains chunk render caches and submits visible draw commands.
/// </summary>
/// <remarks>
/// This API is intended for engine infrastructure and advanced tooling. Prefer <see cref="Runtime.WorldRuntime"/>
/// as the stable gameplay-facing entry point.
/// </remarks>
public sealed class WorldRenderer
{
    private const byte MinVisibleLiquidAmount = 12;
    private readonly ChunkRenderCacheBuilder _cacheBuilder;
    private readonly Dictionary<ChunkCoord, ChunkRenderCache> _chunkRenderCaches = new();
    private readonly WorldRenderSettings _settings;
    private int _buildTick;

    /// <summary>
    /// Creates a world renderer backed by the supplied camera, cache builder, and render settings.
    /// </summary>
    /// <param name="camera">The camera used to determine visibility and screen-space offsets.</param>
    /// <param name="cacheBuilder">The builder used to create chunk render caches.</param>
    /// <param name="settings">The shared render settings.</param>
    public WorldRenderer(Camera2D camera, ChunkRenderCacheBuilder cacheBuilder, WorldRenderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(cacheBuilder);
        ArgumentNullException.ThrowIfNull(settings);

        Camera = camera;
        _cacheBuilder = cacheBuilder;
        _settings = settings;
    }

    public Camera2D Camera { get; }

    /// <summary>
    /// Rebuilds render caches for chunks currently marked as render-dirty.
    /// </summary>
    /// <param name="runtime">The runtime whose dirty chunks should be rebuilt.</param>
    public void RebuildDirtyCaches(WorldRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var rebuiltChunkCoords = new List<ChunkCoord>();
        var dirtyChunkCoords = runtime.DirtyTracker
            .EnumerateDirtyChunks(ChunkDirtyFlags.RenderDirty)
            .ToHashSet();

        if (dirtyChunkCoords.Count == 0)
        {
            return;
        }

        var visibleChunkCoords = GetVisibleChunkCoords().ToArray();
        var visibleChunkSet = visibleChunkCoords.ToHashSet();
        var prioritizedChunkCoords = visibleChunkCoords
            .Where(dirtyChunkCoords.Contains)
            .Concat(dirtyChunkCoords
                .Except(visibleChunkSet)
                .OrderBy(coord => coord.Y)
                .ThenBy(coord => coord.X))
            .Take(_settings.MaxDirtyChunkCacheRebuildsPerFrame)
            .ToArray();

        foreach (var coord in prioritizedChunkCoords)
        {
            if (!runtime.WorldData.TryGetChunk(coord, out var chunk))
            {
                continue;
            }

            var cache = _cacheBuilder.Build(chunk, ++_buildTick);
            _chunkRenderCaches[coord] = cache;
            runtime.DirtyTracker.ClearDirty(coord, ChunkDirtyFlags.RenderDirty);
            rebuiltChunkCoords.Add(coord);
        }

        if (rebuiltChunkCoords.Count > 0)
        {
            EngineDiagnostics.Info($"WorldRenderer rebuilt render caches for: {string.Join(", ", rebuiltChunkCoords)}.");
        }
    }

    /// <summary>
    /// Draws all currently visible chunk caches.
    /// </summary>
    /// <param name="runtime">The runtime that owns the render caches.</param>
    /// <param name="renderContext">The render context that receives visible draw commands.</param>
    public void Draw(WorldRuntime runtime, IRenderContext renderContext)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(renderContext);

        var visibleChunkCoords = GetVisibleChunkCoords().ToArray();

        foreach (var coord in visibleChunkCoords)
        {
            if (!runtime.WorldData.TryGetChunk(coord, out _) ||
                !_chunkRenderCaches.TryGetValue(coord, out var cache) ||
                !cache.IsBuilt)
            {
                continue;
            }

            runtime.LightingSystem.TryGetLightBuffer(coord, out var lightBuffer);

            foreach (var command in cache.BackgroundCommands)
            {
                renderContext.DrawSprite(ToScreenSpace(ApplyChunkLighting(command, cache, lightBuffer)));
            }

            foreach (var command in BuildChunkLiquidCommands(runtime, coord, lightBuffer))
            {
                renderContext.DrawSprite(ToScreenSpace(command));
            }

            foreach (var command in cache.ForegroundCommands)
            {
                renderContext.DrawSprite(ToScreenSpace(ApplyChunkLighting(command, cache, lightBuffer)));
            }
        }

        foreach (var command in BuildVisibleObjectCommands(runtime, visibleChunkCoords))
        {
            renderContext.DrawSprite(command);
        }

        foreach (var command in BuildVisibleEntityCommands(runtime))
        {
            renderContext.DrawSprite(command);
        }
    }

    /// <summary>
    /// Enumerates visible chunk coordinates in stable top-to-bottom, left-to-right order.
    /// </summary>
    /// <returns>An ordered sequence of visible chunk coordinates.</returns>
    public IEnumerable<ChunkCoord> GetVisibleChunkCoords()
    {
        var worldViewBounds = Camera.GetWorldViewBounds();
        var minChunkX = FloorDivide(worldViewBounds.Left, _settings.ChunkWidthPixels) - _settings.VisibleChunkPadding;
        var minChunkY = FloorDivide(worldViewBounds.Top, _settings.ChunkHeightPixels) - _settings.VisibleChunkPadding;
        var maxChunkX = FloorDivide(GetInclusiveRight(worldViewBounds), _settings.ChunkWidthPixels) + _settings.VisibleChunkPadding;
        var maxChunkY = FloorDivide(GetInclusiveBottom(worldViewBounds), _settings.ChunkHeightPixels) + _settings.VisibleChunkPadding;

        for (var chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
        {
            for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
            {
                yield return new ChunkCoord(chunkX, chunkY);
            }
        }
    }

    /// <summary>
    /// Attempts to resolve a previously built render cache for the supplied chunk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to resolve.</param>
    /// <param name="cache">The resolved cache when present.</param>
    /// <returns><see langword="true"/> when a built cache exists for the supplied chunk.</returns>
    public bool TryGetChunkRenderCache(ChunkCoord coord, out ChunkRenderCache cache)
    {
        return _chunkRenderCaches.TryGetValue(coord, out cache!);
    }

    private SpriteDrawCommand ToScreenSpace(SpriteDrawCommand command)
    {
        return command with
        {
            DestinationRectPixels = new RectI(
                command.DestinationRectPixels.X - Camera.PositionPixels.X,
                command.DestinationRectPixels.Y - Camera.PositionPixels.Y,
                command.DestinationRectPixels.Width,
                command.DestinationRectPixels.Height)
        };
    }

    private IEnumerable<SpriteDrawCommand> BuildVisibleObjectCommands(WorldRuntime runtime, IEnumerable<ChunkCoord> visibleChunkCoords)
    {
        var renderedObjectIds = new HashSet<int>();

        foreach (var chunkCoord in visibleChunkCoords)
        {
            foreach (var instance in runtime.ObjectManager.QueryObjectsInChunk(chunkCoord))
            {
                if (!renderedObjectIds.Add(instance.InstanceId) ||
                    !runtime.ContentRegistry.TryGetObjectDef(instance.ObjectDefId, out var objectDef))
                {
                    continue;
                }

                yield return ToScreenSpace(ApplyRectCenterLighting(BuildObjectCommand(runtime, instance, objectDef), runtime));
            }
        }
    }

    private SpriteDrawCommand BuildObjectCommand(WorldRuntime runtime, ObjectInstance instance, ObjectDef objectDef)
    {
        var origin = runtime.ObjectManager.GetFootprintOrigin(instance.AnchorCoord, objectDef);
        return new SpriteDrawCommand(
            objectDef.Visual.TextureKey,
            objectDef.Visual.SourceRect,
            new RectI(
                origin.X * _settings.TileSizePixels,
                origin.Y * _settings.TileSizePixels,
                objectDef.SizeInTiles.X * _settings.TileSizePixels,
                objectDef.SizeInTiles.Y * _settings.TileSizePixels),
            objectDef.Visual.Tint,
            0.3f);
    }

    private IEnumerable<SpriteDrawCommand> BuildVisibleEntityCommands(WorldRuntime runtime)
    {
        var worldViewBounds = Camera.GetWorldViewBounds();

        foreach (var entity in runtime.EnumerateEntities())
        {
            var worldBoundsPixels = new RectI(
                (int)MathF.Round((entity.Position.X + entity.LocalBounds.X) * _settings.TileSizePixels),
                (int)MathF.Round((entity.Position.Y + entity.LocalBounds.Y) * _settings.TileSizePixels),
                Math.Max(1, (int)MathF.Round(entity.LocalBounds.Width * _settings.TileSizePixels)),
                Math.Max(1, (int)MathF.Round(entity.LocalBounds.Height * _settings.TileSizePixels)));

            if (!Intersects(worldBoundsPixels, worldViewBounds))
            {
                continue;
            }

            var visual = ResolveEntityVisual(runtime, entity);

            yield return ToScreenSpace(new SpriteDrawCommand(
                visual.TextureKey,
                visual.SourceRect,
                worldBoundsPixels,
                visual.Tint,
                0.45f)).WithLighting(runtime, worldBoundsPixels, _settings);
        }
    }

    private static (string TextureKey, RectI SourceRect, ColorRgba32 Tint) ResolveEntityVisual(WorldRuntime runtime, Entity entity)
    {
        if (entity.Type == EntityType.Drop &&
            runtime.ContentRegistry.TryGetItemDef(entity.ItemDefId, out var itemDef))
        {
            return (itemDef.Visual.TextureKey, itemDef.Visual.SourceRect, itemDef.Visual.Tint);
        }

        if (entity.Type == EntityType.Player)
        {
            return ("debug/white", new RectI(0, 0, 1, 1), new ColorRgba32(70, 170, 255));
        }

        if (entity.Type == EntityType.Drop)
        {
            return ("debug/white", new RectI(0, 0, 1, 1), new ColorRgba32(255, 240, 140));
        }

        return ("debug/white", new RectI(0, 0, 1, 1), ColorRgba32.White);
    }

    private static bool Intersects(RectI left, RectI right)
    {
        return left.Left < right.Right &&
               left.Right > right.Left &&
               left.Top < right.Bottom &&
               left.Bottom > right.Top;
    }

    private static int GetInclusiveRight(RectI bounds)
    {
        return bounds.Width == 0 ? bounds.Left : bounds.Right - 1;
    }

    private static int GetInclusiveBottom(RectI bounds)
    {
        return bounds.Height == 0 ? bounds.Top : bounds.Bottom - 1;
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

    private SpriteDrawCommand ApplyChunkLighting(
        SpriteDrawCommand command,
        ChunkRenderCache cache,
        ChunkLightBuffer lightBuffer)
    {
        if (lightBuffer is null)
        {
            return ApplyLightLevel(command, 0);
        }

        var localX = (command.DestinationRectPixels.X - cache.WorldPixelBounds.X) / _settings.TileSizePixels;
        var localY = (command.DestinationRectPixels.Y - cache.WorldPixelBounds.Y) / _settings.TileSizePixels;

        if ((uint)localX >= ChunkDimensions.Width || (uint)localY >= ChunkDimensions.Height)
        {
            return ApplyLightLevel(command, 0);
        }

        return ApplyLightLevel(command, lightBuffer.GetLightLevel(localX, localY));
    }

    private SpriteDrawCommand ApplyRectCenterLighting(SpriteDrawCommand command, WorldRuntime runtime)
    {
        var centerPixelX = command.DestinationRectPixels.X + (command.DestinationRectPixels.Width / 2);
        var centerPixelY = command.DestinationRectPixels.Y + (command.DestinationRectPixels.Height / 2);
        var tileCoord = new WorldTileCoord(
            FloorDivide(centerPixelX, _settings.TileSizePixels),
            FloorDivide(centerPixelY, _settings.TileSizePixels));

        return ApplyLightLevel(command, runtime.GetLightLevel(tileCoord));
    }

    private static SpriteDrawCommand ApplyLightLevel(SpriteDrawCommand command, byte lightLevel)
    {
        return command with
        {
            Tint = command.Tint.MultiplyBrightness(lightLevel)
        };
    }

    private IEnumerable<SpriteDrawCommand> BuildChunkLiquidCommands(
        WorldRuntime runtime,
        ChunkCoord chunkCoord,
        ChunkLightBuffer lightBuffer)
    {
        if (!runtime.WorldData.TryGetChunk(chunkCoord, out var chunk))
        {
            yield break;
        }

        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(chunkCoord);

        for (var localY = 0; localY < ChunkDimensions.Height; localY++)
        {
            for (var localX = 0; localX < ChunkDimensions.Width; localX++)
            {
                var cell = chunk.GetCell(localX, localY);
                if (cell.LiquidAmount < MinVisibleLiquidAmount || cell.LiquidType == 0)
                {
                    continue;
                }

                if (cell.ForegroundTileId != 0 &&
                    runtime.ContentRegistry.TryGetTileDef(cell.ForegroundTileId, out var tileDef) &&
                    tileDef.IsSolid)
                {
                    continue;
                }

                var tilePixelX = (chunkOrigin.X + localX) * _settings.TileSizePixels;
                var tilePixelY = (chunkOrigin.Y + localY) * _settings.TileSizePixels;
                var liquidHeight = (int)MathF.Round((cell.LiquidAmount / 255f) * _settings.TileSizePixels);
                if (liquidHeight <= 0)
                {
                    continue;
                }
                var destination = new RectI(
                    tilePixelX,
                    tilePixelY + (_settings.TileSizePixels - liquidHeight),
                    _settings.TileSizePixels,
                    liquidHeight);
                var lightLevel = lightBuffer is not null
                    ? lightBuffer.GetLightLevel(localX, localY)
                    : (byte)0;

                yield return ApplyLightLevel(
                    new SpriteDrawCommand(
                        "debug/white",
                        new RectI(0, 0, 1, 1),
                        destination,
                        ResolveLiquidTint(cell.LiquidType, cell.LiquidAmount),
                        0.08f),
                    lightLevel);
            }
        }
    }

    private static ColorRgba32 ResolveLiquidTint(byte liquidType, byte liquidAmount)
    {
        var alpha = (byte)Math.Clamp(92 + ((liquidAmount * 136) / 255), 0, 255);

        return liquidType switch
        {
            (byte)TileWorld.Engine.World.Cells.LiquidKind.Water => new ColorRgba32(82, 142, 255, alpha),
            (byte)TileWorld.Engine.World.Cells.LiquidKind.Lava => new ColorRgba32(255, 118, 58, alpha),
            (byte)TileWorld.Engine.World.Cells.LiquidKind.Honey => new ColorRgba32(238, 186, 78, alpha),
            _ => new ColorRgba32(82, 142, 255, alpha)
        };
    }
}

internal static class WorldRendererLightingExtensions
{
    public static SpriteDrawCommand WithLighting(
        this SpriteDrawCommand command,
        WorldRuntime runtime,
        RectI worldBoundsPixels,
        WorldRenderSettings settings)
    {
        var centerPixelX = worldBoundsPixels.X + (worldBoundsPixels.Width / 2);
        var centerPixelY = worldBoundsPixels.Y + (worldBoundsPixels.Height / 2);
        var tileCoord = new WorldTileCoord(
            FloorDivide(centerPixelX, settings.TileSizePixels),
            FloorDivide(centerPixelY, settings.TileSizePixels));
        var lightLevel = runtime.GetLightLevel(tileCoord);

        return command with
        {
            Tint = command.Tint.MultiplyBrightness(lightLevel)
        };
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
}
