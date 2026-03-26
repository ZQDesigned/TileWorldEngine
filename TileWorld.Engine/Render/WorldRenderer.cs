using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Entities;
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
            .ToArray();

        foreach (var coord in dirtyChunkCoords)
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

        foreach (var coord in GetVisibleChunkCoords())
        {
            if (!runtime.WorldData.TryGetChunk(coord, out _) ||
                !_chunkRenderCaches.TryGetValue(coord, out var cache) ||
                !cache.IsBuilt)
            {
                continue;
            }

            foreach (var command in cache.BackgroundCommands)
            {
                renderContext.DrawSprite(ToScreenSpace(command));
            }

            foreach (var command in cache.ForegroundCommands)
            {
                renderContext.DrawSprite(ToScreenSpace(command));
            }
        }

        foreach (var command in BuildVisibleObjectCommands(runtime))
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

    private IEnumerable<SpriteDrawCommand> BuildVisibleObjectCommands(WorldRuntime runtime)
    {
        var renderedObjectIds = new HashSet<int>();

        foreach (var chunkCoord in GetVisibleChunkCoords())
        {
            foreach (var instance in runtime.ObjectManager.QueryObjectsInChunk(chunkCoord))
            {
                if (!renderedObjectIds.Add(instance.InstanceId) ||
                    !runtime.ContentRegistry.TryGetObjectDef(instance.ObjectDefId, out var objectDef))
                {
                    continue;
                }

                yield return ToScreenSpace(BuildObjectCommand(runtime, instance, objectDef));
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

            var tint = ResolveEntityTint(runtime, entity);
            var textureKey = ResolveEntityTextureKey(runtime, entity);

            yield return ToScreenSpace(new SpriteDrawCommand(
                textureKey,
                new RectI(0, 0, 1, 1),
                worldBoundsPixels,
                tint,
                0.45f));
        }
    }

    private static string ResolveEntityTextureKey(WorldRuntime runtime, Entity entity)
    {
        return entity.Type == EntityType.Drop &&
               runtime.ContentRegistry.TryGetItemDef(entity.ItemDefId, out var itemDef)
            ? itemDef.Visual.TextureKey
            : "debug/white";
    }

    private static ColorRgba32 ResolveEntityTint(WorldRuntime runtime, Entity entity)
    {
        if (entity.Type == EntityType.Drop &&
            runtime.ContentRegistry.TryGetItemDef(entity.ItemDefId, out var itemDef))
        {
            return itemDef.Visual.Tint;
        }

        return entity.Type switch
        {
            EntityType.Player => new ColorRgba32(70, 170, 255),
            EntityType.Drop => new ColorRgba32(255, 240, 140),
            _ => ColorRgba32.White
        };
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
}
