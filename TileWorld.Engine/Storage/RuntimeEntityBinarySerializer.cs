using System;
using System.Collections.Generic;
using System.IO;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Entities;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Serializes and deserializes persisted non-player runtime entities to the engine's binary save format.
/// </summary>
internal sealed class RuntimeEntityBinarySerializer
{
    private const int CurrentFormatVersion = 1;
    private const uint Magic = 0x4E455754; // "TWEN"

    /// <summary>
    /// Serializes runtime entities into the engine's binary entity-save format.
    /// </summary>
    /// <param name="entities">The entities to serialize.</param>
    /// <returns>The binary payload.</returns>
    /// <remarks>
    /// Engine internal infrastructure API. World persistence flows should prefer <see cref="WorldStorage"/> instead of
    /// calling this serializer directly.
    /// </remarks>
    public byte[] Serialize(IReadOnlyList<Entity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Magic);
        writer.Write(CurrentFormatVersion);
        writer.Write(entities.Count);

        foreach (var entity in entities)
        {
            writer.Write(entity.EntityId);
            writer.Write((int)entity.Type);
            writer.Write(entity.Position.X);
            writer.Write(entity.Position.Y);
            writer.Write(entity.Velocity.X);
            writer.Write(entity.Velocity.Y);
            writer.Write(entity.LocalBounds.X);
            writer.Write(entity.LocalBounds.Y);
            writer.Write(entity.LocalBounds.Width);
            writer.Write(entity.LocalBounds.Height);
            writer.Write((int)(entity.StateFlags & ~EntityStateFlags.PendingRemoval));
            writer.Write(entity.ItemDefId);
            writer.Write(entity.Amount);
        }

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Deserializes runtime entities from the engine's binary entity-save format.
    /// </summary>
    /// <param name="data">The binary payload to deserialize.</param>
    /// <returns>The deserialized entity snapshots.</returns>
    /// <exception cref="InvalidDataException">Thrown when the payload is malformed or uses an unsupported format version.</exception>
    /// <remarks>
    /// Engine internal infrastructure API. World persistence flows should prefer <see cref="WorldStorage"/> instead of
    /// calling this serializer directly.
    /// </remarks>
    public IReadOnlyList<Entity> Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            using var stream = new MemoryStream(data, writable: false);
            using var reader = new BinaryReader(stream);

            var magic = reader.ReadUInt32();
            if (magic != Magic)
            {
                throw new InvalidDataException("Runtime entity binary payload has an invalid magic header.");
            }

            var formatVersion = reader.ReadInt32();
            if (formatVersion != CurrentFormatVersion)
            {
                throw new InvalidDataException(
                    $"Runtime entity binary payload version {formatVersion} is not supported. Expected version {CurrentFormatVersion}.");
            }

            var entityCount = reader.ReadInt32();
            if (entityCount < 0)
            {
                throw new InvalidDataException("Runtime entity binary payload contains a negative entity count.");
            }

            var entities = new Entity[entityCount];
            for (var index = 0; index < entityCount; index++)
            {
                entities[index] = new Entity
                {
                    EntityId = reader.ReadInt32(),
                    Type = (EntityType)reader.ReadInt32(),
                    Position = new Float2(reader.ReadSingle(), reader.ReadSingle()),
                    Velocity = new Float2(reader.ReadSingle(), reader.ReadSingle()),
                    LocalBounds = new AabbF(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                    StateFlags = (EntityStateFlags)reader.ReadInt32() & ~EntityStateFlags.PendingRemoval,
                    ItemDefId = reader.ReadInt32(),
                    Amount = Math.Max(1, reader.ReadInt32())
                };
            }

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException("Runtime entity binary payload contains unexpected trailing data.");
            }

            return entities;
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException("Runtime entity binary payload is truncated.", exception);
        }
    }
}
