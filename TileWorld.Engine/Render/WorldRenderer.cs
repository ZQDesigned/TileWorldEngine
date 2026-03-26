using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

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
            if (!_chunkRenderCaches.TryGetValue(coord, out var cache) || !cache.IsBuilt)
            {
                continue;
            }

            foreach (var command in cache.ForegroundCommands)
            {
                renderContext.DrawSprite(ToScreenSpace(command));
            }
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
