using System;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Generation;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Applies in-memory upgrades to persisted world metadata and chunk payloads before runtime use.
/// </summary>
internal sealed class WorldSaveMigrationService
{
    internal WorldMetadata MigrateMetadata(WorldMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var normalizedGeneratorId = WorldGeneratorIdNormalizer.Normalize(metadata.GeneratorId);

        if (metadata.WorldFormatVersion < 2 ||
            metadata.GeneratorVersion <= 0 ||
            !string.Equals(metadata.GeneratorId, normalizedGeneratorId, StringComparison.Ordinal))
        {
            return CloneMetadata(
                metadata,
                worldFormatVersion: Math.Max(2, metadata.WorldFormatVersion),
                generatorId: normalizedGeneratorId,
                generatorVersion: metadata.GeneratorVersion > 0 ? metadata.GeneratorVersion : 1);
        }

        return metadata;
    }

    internal ChunkStoragePayload MigrateChunkPayload(ChunkStoragePayload payload, WorldMetadata metadata, ChunkCoord coord)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(metadata);
        _ = coord;
        return payload;
    }

    private static WorldMetadata CloneMetadata(
        WorldMetadata metadata,
        int worldFormatVersion,
        string generatorId,
        int generatorVersion)
    {
        return new WorldMetadata
        {
            WorldId = metadata.WorldId,
            Name = metadata.Name,
            Seed = metadata.Seed,
            WorldFormatVersion = worldFormatVersion,
            ChunkFormatVersion = metadata.ChunkFormatVersion,
            GeneratorId = generatorId,
            GeneratorVersion = generatorVersion,
            WorldTime = metadata.WorldTime,
            BoundsMode = metadata.BoundsMode,
            SpawnTile = metadata.SpawnTile,
            MinTileY = metadata.MinTileY,
            MaxTileY = metadata.MaxTileY,
            ChunkWidth = metadata.ChunkWidth,
            ChunkHeight = metadata.ChunkHeight
        };
    }
}
