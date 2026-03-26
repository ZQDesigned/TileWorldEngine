using System.Collections.Generic;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Render;

/// <summary>
/// Builds render caches for chunk foreground tiles.
/// </summary>
/// <remarks>
/// This API is intended for engine infrastructure and advanced tooling. Most callers should use
/// <see cref="WorldRenderer"/> instead of building chunk caches directly.
/// </remarks>
public sealed class ChunkRenderCacheBuilder
{
    private readonly ContentRegistry _contentRegistry;
    private readonly WorldRenderSettings _settings;

    /// <summary>
    /// Creates a render-cache builder.
    /// </summary>
    /// <param name="contentRegistry">The registry used to resolve tile visuals.</param>
    /// <param name="settings">The shared render settings.</param>
    public ChunkRenderCacheBuilder(ContentRegistry contentRegistry, WorldRenderSettings settings)
    {
        _contentRegistry = contentRegistry;
        _settings = settings;
    }

    /// <summary>
    /// Builds a render cache for a chunk using its current cell contents.
    /// </summary>
    /// <param name="chunk">The chunk to scan.</param>
    /// <param name="buildTick">The monotonic build tick assigned to the cache.</param>
    /// <returns>A render cache containing all generated foreground draw commands.</returns>
    public ChunkRenderCache Build(Chunk chunk, int buildTick)
    {
        var commands = new List<SpriteDrawCommand>();
        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(chunk.Coord);
        var worldPixelBounds = new RectI(
            chunkOrigin.X * _settings.TileSizePixels,
            chunkOrigin.Y * _settings.TileSizePixels,
            _settings.ChunkWidthPixels,
            _settings.ChunkHeightPixels);

        for (var localY = 0; localY < ChunkDimensions.Height; localY++)
        {
            for (var localX = 0; localX < ChunkDimensions.Width; localX++)
            {
                var cell = chunk.GetCell(localX, localY);
                if (cell.ForegroundTileId == 0)
                {
                    continue;
                }

                if (!_contentRegistry.TryGetTileDef(cell.ForegroundTileId, out var tileDef))
                {
                    continue;
                }

                var destinationRectPixels = new RectI(
                    worldPixelBounds.X + (localX * _settings.TileSizePixels),
                    worldPixelBounds.Y + (localY * _settings.TileSizePixels),
                    _settings.TileSizePixels,
                    _settings.TileSizePixels);

                commands.Add(new SpriteDrawCommand(
                    tileDef.Visual.TextureKey,
                    ResolveSourceRect(tileDef.Visual, cell.Variant),
                    destinationRectPixels,
                    tileDef.Visual.Tint,
                    0f));
            }
        }

        return new ChunkRenderCache(chunk.Coord, true, buildTick, worldPixelBounds, commands);
    }

    private RectI ResolveSourceRect(TileVisualDef visual, ushort variant)
    {
        if (!visual.UseVariantHorizontalStrip)
        {
            return visual.SourceRect;
        }

        return visual.SourceRect with
        {
            X = visual.SourceRect.X + (variant * _settings.TileSizePixels)
        };
    }
}
