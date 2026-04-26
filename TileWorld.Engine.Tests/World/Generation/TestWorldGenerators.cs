using System;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Generation;

namespace TileWorld.Engine.Tests.World.Generation;

internal sealed class FlatTestWorldGenerator : IWorldGenerator
{
    internal const string GeneratorIdValue = "flat_test";

    public string GeneratorId => GeneratorIdValue;

    public int GeneratorVersion => 1;

    public ChunkGenerationResult GenerateChunk(WorldGenerationContext context, ChunkCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunk = new Chunk(coord);
        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(coord);
        var surfaceY = context.Metadata.SpawnTile.Y + 2;

        for (var localY = 0; localY < ChunkDimensions.Height; localY++)
        {
            var worldY = chunkOrigin.Y + localY;
            if (!WorldVerticalBoundsUtility.IsTileYWithinBounds(context.Metadata, worldY) ||
                worldY < surfaceY)
            {
                continue;
            }

            for (var localX = 0; localX < ChunkDimensions.Width; localX++)
            {
                var depthBelowSurface = worldY - surfaceY;
                chunk.SetCell(localX, localY, new TileCell
                {
                    ForegroundTileId = depthBelowSurface <= 1 ? (ushort)2 : (ushort)1,
                    BackgroundWallId = depthBelowSurface > 0 ? (ushort)2 : (ushort)0
                });
            }
        }

        return new ChunkGenerationResult(chunk);
    }

    public int GetSurfaceHeight(WorldGenerationContext context, int worldX)
    {
        ArgumentNullException.ThrowIfNull(context);
        _ = worldX;
        return context.Metadata.SpawnTile.Y + 2;
    }

    public int GetBiomeId(WorldGenerationContext context, WorldTileCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);
        _ = coord;
        return 1;
    }
}

internal sealed class DeterministicTerrainTestWorldGenerator : IWorldGenerator
{
    internal const string GeneratorIdValue = "deterministic_test";

    public string GeneratorId => GeneratorIdValue;

    public int GeneratorVersion => 3;

    public ChunkGenerationResult GenerateChunk(WorldGenerationContext context, ChunkCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunk = new Chunk(coord);
        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(coord);

        for (var localX = 0; localX < ChunkDimensions.Width; localX++)
        {
            var worldX = chunkOrigin.X + localX;
            var surfaceY = GetSurfaceHeight(context, worldX);

            for (var localY = 0; localY < ChunkDimensions.Height; localY++)
            {
                var worldY = chunkOrigin.Y + localY;
                if (!WorldVerticalBoundsUtility.IsTileYWithinBounds(context.Metadata, worldY) ||
                    worldY < surfaceY)
                {
                    continue;
                }

                var depthBelowSurface = worldY - surfaceY;
                chunk.SetCell(localX, localY, new TileCell
                {
                    ForegroundTileId = depthBelowSurface <= 1 ? (ushort)2 : (ushort)1,
                    BackgroundWallId = depthBelowSurface > 0 ? (ushort)2 : (ushort)0
                });
            }
        }

        return new ChunkGenerationResult(chunk);
    }

    public int GetSurfaceHeight(WorldGenerationContext context, int worldX)
    {
        ArgumentNullException.ThrowIfNull(context);

        var baseSurface = context.Metadata.SpawnTile.Y + 2;
        var offset = SampleSignedNoise(worldX, context.Metadata.Seed, 1709) * 4f;
        return baseSurface + (int)MathF.Round(offset);
    }

    public int GetBiomeId(WorldGenerationContext context, WorldTileCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SampleSignedNoise(coord.X, context.Metadata.Seed, 2203) >= 0f ? 1 : 2;
    }

    private static float SampleSignedNoise(int value, int seed, int salt)
    {
        unchecked
        {
            var hash = seed;
            hash = (hash * 397) ^ value;
            hash = (hash * 397) ^ salt;
            hash ^= hash >> 13;
            hash *= 1274126177;
            hash ^= hash >> 16;
            var normalized = (hash & 0x7fffffff) / (float)int.MaxValue;
            return (normalized * 2f) - 1f;
        }
    }
}

internal static class TestWorldGeneratorRegistryFactory
{
    internal static WorldGeneratorRegistry CreateFlatRegistry()
    {
        var registry = new WorldGeneratorRegistry();
        var flatGenerator = new FlatTestWorldGenerator();
        registry.Register(flatGenerator);
        return registry;
    }
}
