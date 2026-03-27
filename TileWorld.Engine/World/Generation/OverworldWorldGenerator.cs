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
    private const int SpawnFlatHalfWidth = 18;
    private const int SpawnProtectedHalfWidth = 26;

    public string GeneratorId => WorldGeneratorIdNormalizer.Overworld;

    public int GeneratorVersion => 1;

    public ChunkGenerationResult GenerateChunk(WorldGenerationContext context, ChunkCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunk = new Chunk(coord);
        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(coord);

        for (var localX = 0; localX < ChunkDimensions.Width; localX++)
        {
            var worldX = chunkOrigin.X + localX;
            var biomeId = GetBiomeId(context, new WorldTileCoord(worldX, context.Metadata.SpawnTile.Y));
            var biome = ResolveBiomeDef(context.ContentRegistry, biomeId);
            var surfaceY = GetSurfaceHeight(context, worldX, biomeId);
            var topSoilDepth = GetTopSoilDepth(context, worldX, biomeId);
            var surfaceTileId = ResolveTileId(context.ContentRegistry, biome.SurfaceTileId, fallbackTileId: 1);
            var subsurfaceTileId = ResolveTileId(context.ContentRegistry, biome.SubsurfaceTileId, fallbackTileId: surfaceTileId);
            var deepStoneTileId = ResolveTileId(context.ContentRegistry, preferredTileId: 1, fallbackTileId: subsurfaceTileId);
            var oreTileId = ResolveTileId(context.ContentRegistry, preferredTileId: 3, fallbackTileId: deepStoneTileId);

            for (var localY = 0; localY < ChunkDimensions.Height; localY++)
            {
                var worldY = chunkOrigin.Y + localY;
                if (!WorldVerticalBoundsUtility.IsTileYWithinBounds(context.Metadata, worldY))
                {
                    continue;
                }

                var depthBelowSurface = worldY - surfaceY;
                if (depthBelowSurface < 0)
                {
                    continue;
                }

                if (ShouldCarveCave(context, worldX, worldY, depthBelowSurface))
                {
                    chunk.SetCell(localX, localY, new TileCell
                    {
                        BackgroundWallId = depthBelowSurface > 1 ? biome.SurfaceWallId : (ushort)0
                    });
                    continue;
                }

                var foregroundTileId = depthBelowSurface <= topSoilDepth
                    ? surfaceTileId
                    : depthBelowSurface <= topSoilDepth + 6
                        ? subsurfaceTileId
                        : deepStoneTileId;

                if (foregroundTileId == deepStoneTileId &&
                    depthBelowSurface > 18 &&
                    ShouldPlaceOre(context, worldX, worldY, depthBelowSurface))
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
        if (Math.Abs(coord.X - spawnX) <= 72)
        {
            return PlainsBiomeId;
        }

        var continentalNoise = SampleSmoothNoise1D(coord.X, context.Metadata.Seed, wavelength: 192f, salt: 7101);
        var ridgeNoise = SampleSmoothNoise1D(coord.X, context.Metadata.Seed, wavelength: 96f, salt: 7201);
        return (continentalNoise + (ridgeNoise * 0.55f)) > 0.18f
            ? RockyBiomeId
            : PlainsBiomeId;
    }

    private static int GetSurfaceHeight(WorldGenerationContext context, int worldX, int biomeId)
    {
        var baseSurfaceY = context.Metadata.SpawnTile.Y + 2;
        var distanceFromSpawn = Math.Abs(worldX - context.Metadata.SpawnTile.X);
        if (distanceFromSpawn <= SpawnFlatHalfWidth)
        {
            return baseSurfaceY;
        }

        var offset = (
            GetRawSurfaceOffset(context, worldX - 2, biomeId) +
            (GetRawSurfaceOffset(context, worldX - 1, biomeId) * 2f) +
            (GetRawSurfaceOffset(context, worldX, biomeId) * 3f) +
            (GetRawSurfaceOffset(context, worldX + 1, biomeId) * 2f) +
            GetRawSurfaceOffset(context, worldX + 2, biomeId)) / 9f;

        return baseSurfaceY + (int)MathF.Round(offset);
    }

    private static float GetRawSurfaceOffset(WorldGenerationContext context, int worldX, int biomeId)
    {
        var distanceFromSpawn = Math.Abs(worldX - context.Metadata.SpawnTile.X);
        var spawnBlend = SmoothStep(SpawnFlatHalfWidth, SpawnFlatHalfWidth + 24, distanceFromSpawn);
        var biomeRoughness = biomeId == RockyBiomeId ? 1.2f : 0.82f;
        var continental = SampleSmoothNoise1D(worldX, context.Metadata.Seed, wavelength: 176f, salt: 1101) * 5.25f;
        var hills = SampleSmoothNoise1D(worldX, context.Metadata.Seed, wavelength: 72f, salt: 1201) * (3f * biomeRoughness);
        var detail = SampleSmoothNoise1D(worldX, context.Metadata.Seed, wavelength: 32f, salt: 1301) * (0.7f * biomeRoughness);
        var valleySignal = SampleNormalizedNoise1D(worldX, context.Metadata.Seed, wavelength: 224f, salt: 1401);
        var valleyDepth = valleySignal > 0.8f
            ? -((valleySignal - 0.8f) / 0.2f) * (biomeId == RockyBiomeId ? 4f : 2.75f)
            : 0f;
        var biomeBias = biomeId == RockyBiomeId ? 1.25f : -0.35f;
        return (continental + hills + detail + valleyDepth + biomeBias) * spawnBlend;
    }

    private static int GetTopSoilDepth(WorldGenerationContext context, int worldX, int biomeId)
    {
        var organicDepth = biomeId == RockyBiomeId ? 1.5f : 3.5f;
        var variation = SampleNormalizedNoise1D(worldX, context.Metadata.Seed, wavelength: 40f, salt: 2101);
        return Math.Max(1, (int)MathF.Round(organicDepth + (variation * 2f)));
    }

    private static bool ShouldCarveCave(WorldGenerationContext context, int worldX, int worldY, int depthBelowSurface)
    {
        if (depthBelowSurface <= 9)
        {
            return false;
        }

        if (Math.Abs(worldX - context.Metadata.SpawnTile.X) <= SpawnProtectedHalfWidth)
        {
            return false;
        }

        var tunnelNoise = MathF.Abs(SampleSmoothNoise2D(worldX, worldY, context.Metadata.Seed, wavelength: 26f, salt: 3101));
        var cavernNoise = SampleNormalizedNoise2D(worldX, worldY, context.Metadata.Seed, wavelength: 58f, salt: 3201);
        var pocketNoise = SampleNormalizedNoise2D(worldX, worldY, context.Metadata.Seed, wavelength: 18f, salt: 3301);
        var depthFactor = Math.Clamp((depthBelowSurface - 10) / 28f, 0f, 1f);
        var canOpenTunnel = tunnelNoise < (0.055f + (depthFactor * 0.035f));
        var canOpenCavern = cavernNoise > (0.79f - (depthFactor * 0.12f)) && pocketNoise > 0.42f;

        return canOpenTunnel || canOpenCavern;
    }

    private static bool ShouldPlaceOre(WorldGenerationContext context, int worldX, int worldY, int depthBelowSurface)
    {
        if (depthBelowSurface <= 18)
        {
            return false;
        }

        var clusterNoise = SampleNormalizedNoise2D(worldX, worldY, context.Metadata.Seed, wavelength: 22f, salt: 4101);
        var scatterNoise = SampleNormalizedNoise2D(worldX, worldY, context.Metadata.Seed, wavelength: 9f, salt: 4201);
        var richnessBias = Math.Clamp((depthBelowSurface - 18) / 40f, 0f, 0.12f);
        return clusterNoise > (0.84f - richnessBias) && scatterNoise > 0.45f;
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

    private static float SampleSmoothNoise1D(int x, int seed, float wavelength, int salt)
    {
        var scaledX = x / wavelength;
        var left = (int)MathF.Floor(scaledX);
        var t = scaledX - left;

        var a = SampleSignedNoise(left, 0, seed, salt);
        var b = SampleSignedNoise(left + 1, 0, seed, salt);
        return Lerp(a, b, SmoothStep01(t));
    }

    private static float SampleSmoothNoise2D(int x, int y, int seed, float wavelength, int salt)
    {
        var scaledX = x / wavelength;
        var scaledY = y / wavelength;
        var left = (int)MathF.Floor(scaledX);
        var top = (int)MathF.Floor(scaledY);
        var tx = SmoothStep01(scaledX - left);
        var ty = SmoothStep01(scaledY - top);

        var topLeft = SampleSignedNoise(left, top, seed, salt);
        var topRight = SampleSignedNoise(left + 1, top, seed, salt);
        var bottomLeft = SampleSignedNoise(left, top + 1, seed, salt);
        var bottomRight = SampleSignedNoise(left + 1, top + 1, seed, salt);
        var topBlend = Lerp(topLeft, topRight, tx);
        var bottomBlend = Lerp(bottomLeft, bottomRight, tx);
        return Lerp(topBlend, bottomBlend, ty);
    }

    private static float SampleNormalizedNoise1D(int x, int seed, float wavelength, int salt)
    {
        return (SampleSmoothNoise1D(x, seed, wavelength, salt) + 1f) * 0.5f;
    }

    private static float SampleNormalizedNoise2D(int x, int y, int seed, float wavelength, int salt)
    {
        return (SampleSmoothNoise2D(x, y, seed, wavelength, salt) + 1f) * 0.5f;
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        if (edge0 >= edge1)
        {
            return value >= edge1 ? 1f : 0f;
        }

        return SmoothStep01(Math.Clamp((value - edge0) / (edge1 - edge0), 0f, 1f));
    }

    private static float SmoothStep01(float value)
    {
        var clamped = Math.Clamp(value, 0f, 1f);
        return clamped * clamped * (3f - (2f * clamped));
    }

    private static float Lerp(float a, float b, float amount)
    {
        return a + ((b - a) * amount);
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
