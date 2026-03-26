using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Entities;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Serializes and deserializes persisted entity payloads to the engine's JSON save format.
/// </summary>
internal sealed class EntityPersistenceSerializer
{
    private const int CurrentFormatVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes persisted entities into the engine's runtime-entity JSON format.
    /// </summary>
    /// <param name="entities">The entities to serialize.</param>
    /// <returns>The JSON payload.</returns>
    /// <remarks>
    /// Engine internal infrastructure API. World persistence flows should prefer <see cref="WorldStorage"/> instead of
    /// calling this serializer directly.
    /// </remarks>
    public string Serialize(IReadOnlyList<Entity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var dto = new EntityPersistenceDto
        {
            FormatVersion = CurrentFormatVersion,
            Entities = entities
                .Select(static entity => new EntityDto
                {
                    EntityId = entity.EntityId,
                    Type = entity.Type,
                    Position = new Float2Dto { X = entity.Position.X, Y = entity.Position.Y },
                    Velocity = new Float2Dto { X = entity.Velocity.X, Y = entity.Velocity.Y },
                    LocalBounds = new AabbFDto
                    {
                        X = entity.LocalBounds.X,
                        Y = entity.LocalBounds.Y,
                        Width = entity.LocalBounds.Width,
                        Height = entity.LocalBounds.Height
                    },
                    StateFlags = entity.StateFlags,
                    ItemDefId = entity.ItemDefId,
                    Amount = entity.Amount
                })
                .ToArray()
        };

        return JsonSerializer.Serialize(dto, SerializerOptions);
    }

    /// <summary>
    /// Deserializes persisted entities from the engine's runtime-entity JSON format.
    /// </summary>
    /// <param name="json">The JSON payload to deserialize.</param>
    /// <returns>The deserialized entity snapshots.</returns>
    /// <exception cref="InvalidDataException">Thrown when the payload is malformed or uses an unsupported format version.</exception>
    /// <remarks>
    /// Engine internal infrastructure API. World persistence flows should prefer <see cref="WorldStorage"/> instead of
    /// calling this serializer directly.
    /// </remarks>
    public IReadOnlyList<Entity> Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            var dto = JsonSerializer.Deserialize<EntityPersistenceDto>(json, SerializerOptions)
                ?? throw new InvalidDataException("Entity persistence content is empty or invalid.");

            if (dto.FormatVersion != CurrentFormatVersion)
            {
                throw new InvalidDataException(
                    $"Entity persistence format version {dto.FormatVersion} is not supported. Expected version {CurrentFormatVersion}.");
            }

            return dto.Entities
                .Select(static entity => new Entity
                {
                    EntityId = entity.EntityId,
                    Type = entity.Type,
                    Position = new Float2(entity.Position?.X ?? 0f, entity.Position?.Y ?? 0f),
                    Velocity = new Float2(entity.Velocity?.X ?? 0f, entity.Velocity?.Y ?? 0f),
                    LocalBounds = new AabbF(
                        entity.LocalBounds?.X ?? 0f,
                        entity.LocalBounds?.Y ?? 0f,
                        entity.LocalBounds?.Width ?? 1f,
                        entity.LocalBounds?.Height ?? 1f),
                    StateFlags = entity.StateFlags & ~EntityStateFlags.PendingRemoval,
                    ItemDefId = entity.ItemDefId,
                    Amount = Math.Max(1, entity.Amount)
                })
                .ToArray();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Entity persistence JSON is invalid.", exception);
        }
    }

    private sealed class EntityPersistenceDto
    {
        public int FormatVersion { get; set; }

        public EntityDto[] Entities { get; set; } = [];
    }

    private sealed class EntityDto
    {
        public int EntityId { get; set; }

        public EntityType Type { get; set; }

        public Float2Dto Position { get; set; } = new();

        public Float2Dto Velocity { get; set; } = new();

        public AabbFDto LocalBounds { get; set; } = new();

        public EntityStateFlags StateFlags { get; set; }

        public int ItemDefId { get; set; }

        public int Amount { get; set; }
    }

    private sealed class Float2Dto
    {
        public float X { get; set; }

        public float Y { get; set; }
    }

    private sealed class AabbFDto
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }
    }
}
