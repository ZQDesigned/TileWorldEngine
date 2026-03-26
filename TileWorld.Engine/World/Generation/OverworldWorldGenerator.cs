using System;
using TileWorld.Engine.Content.Biomes;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World.Generation;

internal sealed class OverworldWorldGenerator : IWorldGenerator
{
    private const int PlainsBiomeId = 1;
    private const int RockyBiomeId = 2;

    public string GeneratorId => "overworld_v1";

    public int GeneratorVersion => 1;

    public ChunkGenerationResult GenerateChunk(WorldGenerationContext context, ChunkCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunk = new Chunk(coord);
        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(coord);

        for (var localY = 0; localY < ChunkDimensions.Height; localY++)
        {
            var worldY = chunkOrigin.Y + localY;
            if (!WorldVerticalBoundsUtility.IsTileYWithinBounds(context.Metadata, worldY))
            {
                continue;
            }

            for (var localX = 0; localX < ChunkDimensions.Width; localX++)
            {
                var worldX = chunkOrigin.X + localX;
                var biomeId = GetBiomeId(context, new WorldTileCoord(worldX, worldY));
                var biome = ResolveBiomeDef(context.ContentRegistry, biomeId);
                var surfaceY = GetSurfaceHeight(context, worldX, biomeId);
                var depthBelowSurface = worldY - surfaceY;
                if (depthBelowSurface < 0)
                {
                    continue;
                }

                var cave = ShouldCarveCave(context, worldX, worldY, surfaceY);
                if (cave)
                {
                    chunk.SetCell(localX, localY, new TileCell
                    {
                        BackgroundWallId = biome.SurfaceWallId
                    });
                    continue;
                }

                var surfaceTileId = ResolveTileId(context.ContentRegistry, biome.SurfaceTileId, fallbackTileId: 1);
                var subsurfaceTileId = ResolveTileId(context.ContentRegistry, biome.SubsurfaceTileId, fallbackTileId: surfaceTileId);
                var deepStoneTileId = ResolveTileId(context.ContentRegistry, preferredTileId: 1, fallbackTileId: subsurfaceTileId);
                var oreTileId = ResolveTileId(context.ContentRegistry, preferredTileId: 3, fallbackTileId: deepStoneTileId);
                var foregroundTileId = depthBelowSurface == 0
                    ? surfaceTileId
                    : depthBelowSurface <= 3
                        ? subsurfaceTileId
                        : deepStoneTileId;
                if (foregroundTileId == deepStoneTileId &&
                    depthBelowSurface > 12 &&
                    ShouldPlaceOre(context, worldX, worldY))
                {
                    foregroundTileId = oreTileId;
                }

                chunk.SetCell(localX, localY, new TileCell
                {
                    ForegroundTileId = foregroundTileId,
                    BackgroundWallId = depthBelowSurface > 0 ? biome.SurfaceWallId : (ushort)0
                });
            }
        }

        return new ChunkGenerationResult(chunk);
    }

    public int GetBiomeId(WorldGenerationContext context, WorldTileCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);

        var spawnX = context.Metadata.SpawnTile.X;
        if (Math.Abs(coord.X - spawnX) <= 48)
        {
            return PlainsBiomeId;
        }

        var largeScaleNoise = SampleSignedNoise(coord.X, 0, context.Metadata.Seed, 7101);
        var wave = MathF.Sin((coord.X + (context.Metadata.Seed * 0.17f)) * 0.0125f);
        return (largeScaleNoise + (wave * 0.75f)) > 0.2f
            ? RockyBiomeId
            : PlainsBiomeId;
    }

    private static int GetSurfaceHeight(WorldGenerationContext context, int worldX, int biomeId)
    {
        var baseSurfaceY = context.Metadata.SpawnTile.Y + 2;
        var distanceFromSpawn = Math.Abs(worldX - context.Metadata.SpawnTile.X);
        if (distanceFromSpawn <= 12)
        {
            return baseSurfaceY;
        }

        var waveA = MathF.Sin((worldX + (context.Metadata.Seed * 0.11f)) * 0.065f) * 2.5f;
        var waveB = MathF.Sin((worldX - (context.Metadata.Seed * 0.03f)) * 0.018f) * 4f;
        var noise = SampleSignedNoise(worldX, biomeId, context.Metadata.Seed, 1049) * 2f;
        var biomeBias = biomeId == RockyBiomeId ? 1.5f : 0f;
        return baseSurfaceY + (int)MathF.Round(waveA + waveB + noise + biomeBias);
    }

    private static bool ShouldCarveCave(WorldGenerationContext context, int worldX, int worldY, int surfaceY)
    {
        if (worldY <= surfaceY + 6)
        {
            return false;
        }

        if (Math.Abs(worldX - context.Metadata.SpawnTile.X) <= 12)
        {
            return false;
        }

        var noise =
            SampleSignedNoise(worldX, worldY, context.Metadata.Seed, 901) +
            (SampleSignedNoise(worldX / 2, worldY / 2, context.Metadata.Seed, 1901) * 0.65f) +
            (SampleSignedNoise(worldX / 4, worldY / 4, context.Metadata.Seed, 2901) * 0.35f);
        return noise > 0.92f;
    }

    private static bool ShouldPlaceOre(WorldGenerationContext context, int worldX, int worldY)
    {
        return SampleSignedNoise(worldX, worldY, context.Metadata.Seed, 4903) > 0.96f;
    }

    private static BiomeDef ResolveBiomeDef(ContentRegistry contentRegistry, int biomeId)
    {
        if (contentRegistry.TryGetBiomeDef(biomeId, out var biomeDef))
        {
            return biomeDef;
        }

        return biomeId == RockyBiomeId
            ? new BiomeDef
            {
                Id = RockyBiomeId,
                Name = "Rocky",
                SurfaceTileId = 1,
                SubsurfaceTileId = 1,
                SurfaceWallId = 1,
                Priority = 10
            }
            : new BiomeDef
            {
                Id = PlainsBiomeId,
                Name = "Plains",
                SurfaceTileId = 2,
                SubsurfaceTileId = 2,
                SurfaceWallId = 2,
                Priority = 5
            };
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

    private static float SampleSignedNoise(int x, int y, int seed, int salt)
    {
        unchecked
        {
            var hash = seed;
            hash = (hash * 397) ^ x;
            hash = (hash * 397) ^ y;
            hash = (hash * 397) ^ salt;
            hash ^= hash >> 13;
            hash *= 1274126177;
            hash ^= hash >> 16;
            var normalized = (hash & 0x7fffffff) / (float)int.MaxValue;
            return (normalized * 2f) - 1f;
        }
    }
}
