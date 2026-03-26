using System;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World.Generation;

internal sealed class FlatDebugWorldGenerator : IWorldGenerator
{
    public string GeneratorId => "flat_debug_v1";

    public int GeneratorVersion => 1;

    public ChunkGenerationResult GenerateChunk(WorldGenerationContext context, ChunkCoord coord)
    {
        return GenerateFlatChunk(context, coord, topTileId: 2, subsurfaceTileId: 1, wallId: 2);
    }

    public int GetBiomeId(WorldGenerationContext context, WorldTileCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);
        _ = coord;
        return 1;
    }

    internal static ChunkGenerationResult GenerateFlatChunk(
        WorldGenerationContext context,
        ChunkCoord coord,
        ushort topTileId,
        ushort subsurfaceTileId,
        ushort wallId)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunk = new Chunk(coord);
        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(coord);
        var surfaceY = context.Metadata.SpawnTile.Y + 2;
        var resolvedTopTileId = ResolveTileId(context.ContentRegistry, topTileId, fallbackTileId: 1);
        var resolvedSubsurfaceTileId = ResolveTileId(context.ContentRegistry, subsurfaceTileId, fallbackTileId: resolvedTopTileId);
        var resolvedWallId = ResolveWallId(context.ContentRegistry, wallId, fallbackWallId: 1);

        for (var localY = 0; localY < ChunkDimensions.Height; localY++)
        {
            var worldY = chunkOrigin.Y + localY;
            if (worldY < surfaceY)
            {
                continue;
            }

            for (var localX = 0; localX < ChunkDimensions.Width; localX++)
            {
                var depthBelowSurface = worldY - surfaceY;
                chunk.SetCell(localX, localY, new TileCell
                {
                    ForegroundTileId = depthBelowSurface <= 1 ? resolvedTopTileId : resolvedSubsurfaceTileId,
                    BackgroundWallId = depthBelowSurface > 0 ? resolvedWallId : (ushort)0
                });
            }
        }

        return new ChunkGenerationResult(chunk);
    }

    private static ushort ResolveTileId(ContentRegistry contentRegistry, ushort preferredTileId, ushort fallbackTileId)
    {
        if (preferredTileId != 0 && contentRegistry.HasTileDef(preferredTileId))
        {
            return preferredTileId;
        }

        return fallbackTileId != 0 && contentRegistry.HasTileDef(fallbackTileId)
            ? fallbackTileId
            : (ushort)0;
    }

    private static ushort ResolveWallId(ContentRegistry contentRegistry, ushort preferredWallId, ushort fallbackWallId)
    {
        if (preferredWallId != 0 && contentRegistry.HasWallDef(preferredWallId))
        {
            return preferredWallId;
        }

        return fallbackWallId != 0 && contentRegistry.HasWallDef(fallbackWallId)
            ? fallbackWallId
            : (ushort)0;
    }
}
