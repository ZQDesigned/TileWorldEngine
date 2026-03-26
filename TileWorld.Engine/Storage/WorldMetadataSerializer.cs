using System;
using System.IO;
using System.Text.Json;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Serializes and deserializes world metadata to the persistent JSON format.
/// </summary>
public sealed class WorldMetadataSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes world metadata into the engine's persistent JSON format.
    /// </summary>
    /// <param name="metadata">The metadata to serialize.</param>
    /// <returns>The JSON payload.</returns>
    public string Serialize(WorldMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var dto = new WorldMetadataDto
        {
            WorldId = metadata.WorldId,
            Name = metadata.Name,
            Seed = metadata.Seed,
            WorldFormatVersion = metadata.WorldFormatVersion,
            ChunkFormatVersion = metadata.ChunkFormatVersion,
            GeneratorId = metadata.GeneratorId,
            GeneratorVersion = metadata.GeneratorVersion,
            WorldTime = metadata.WorldTime,
            BoundsMode = metadata.BoundsMode,
            SpawnTile = new Int2Dto
            {
                X = metadata.SpawnTile.X,
                Y = metadata.SpawnTile.Y
            },
            ChunkWidth = metadata.ChunkWidth,
            ChunkHeight = metadata.ChunkHeight
        };

        return JsonSerializer.Serialize(dto, SerializerOptions);
    }

    /// <summary>
    /// Deserializes world metadata from the engine's persistent JSON format.
    /// </summary>
    /// <param name="json">The JSON payload to deserialize.</param>
    /// <returns>The deserialized world metadata.</returns>
    /// <exception cref="InvalidDataException">Thrown when the payload is malformed or incompatible with runtime chunk dimensions.</exception>
    public WorldMetadata Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            var dto = JsonSerializer.Deserialize<WorldMetadataDto>(json, SerializerOptions)
                ?? throw new InvalidDataException("World metadata content is empty or invalid.");

            ValidateChunkDimensions(dto.ChunkWidth, dto.ChunkHeight);

            return new WorldMetadata
            {
                WorldId = dto.WorldId ?? string.Empty,
                Name = dto.Name ?? string.Empty,
                Seed = dto.Seed,
                WorldFormatVersion = dto.WorldFormatVersion,
                ChunkFormatVersion = dto.ChunkFormatVersion,
                GeneratorId = dto.GeneratorId ?? string.Empty,
                GeneratorVersion = dto.GeneratorVersion,
                WorldTime = dto.WorldTime,
                BoundsMode = dto.BoundsMode,
                SpawnTile = new Int2(dto.SpawnTile?.X ?? 0, dto.SpawnTile?.Y ?? 0),
                ChunkWidth = dto.ChunkWidth,
                ChunkHeight = dto.ChunkHeight
            };
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("World metadata JSON is invalid.", exception);
        }
    }

    private static void ValidateChunkDimensions(int chunkWidth, int chunkHeight)
    {
        if (chunkWidth != ChunkDimensions.Width || chunkHeight != ChunkDimensions.Height)
        {
            throw new InvalidDataException(
                $"World metadata chunk dimensions ({chunkWidth}, {chunkHeight}) do not match the runtime chunk dimensions ({ChunkDimensions.Width}, {ChunkDimensions.Height}).");
        }
    }

    private sealed class WorldMetadataDto
    {
        public string WorldId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Seed { get; set; }

        public int WorldFormatVersion { get; set; }

        public int ChunkFormatVersion { get; set; }

        public string GeneratorId { get; set; } = string.Empty;

        public int GeneratorVersion { get; set; }

        public long WorldTime { get; set; }

        public WorldBoundsMode BoundsMode { get; set; } = WorldBoundsMode.LargeFinite;

        public Int2Dto SpawnTile { get; set; } = new();

        public int ChunkWidth { get; set; }

        public int ChunkHeight { get; set; }
    }

    private sealed class Int2Dto
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}
