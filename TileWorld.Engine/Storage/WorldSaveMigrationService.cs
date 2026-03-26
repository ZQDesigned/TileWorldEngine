using System;
using TileWorld.Engine.World;
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

        if (metadata.WorldFormatVersion <= 1 &&
            string.IsNullOrWhiteSpace(metadata.GeneratorId))
        {
            return CloneMetadata(
                metadata,
                worldFormatVersion: 2,
                generatorId: "legacy_flat_v1",
                generatorVersion: 1);
        }

        if (metadata.WorldFormatVersion < 2 ||
            string.IsNullOrWhiteSpace(metadata.GeneratorId) ||
            metadata.GeneratorVersion <= 0)
        {
            return CloneMetadata(
                metadata,
                worldFormatVersion: Math.Max(2, metadata.WorldFormatVersion),
                generatorId: string.IsNullOrWhiteSpace(metadata.GeneratorId) ? "overworld_v1" : metadata.GeneratorId,
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
