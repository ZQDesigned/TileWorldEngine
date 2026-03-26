using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Objects;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Serializes and deserializes chunk cell data to the on-disk binary chunk format.
/// </summary>
public sealed class ChunkSerializer
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TWCH");

    /// <summary>
    /// Serializes a chunk into the engine's binary chunk payload format.
    /// </summary>
    /// <param name="chunk">The chunk to serialize.</param>
    /// <returns>The binary chunk payload.</returns>
    public byte[] Serialize(Chunk chunk)
    {
        return Serialize(chunk, []);
    }

    /// <summary>
    /// Serializes a chunk and its anchored object instances into the engine's binary chunk payload format.
    /// </summary>
    /// <param name="chunk">The chunk to serialize.</param>
    /// <param name="anchoredObjects">The anchored object instances that should be stored with this chunk.</param>
    /// <returns>The binary chunk payload.</returns>
    public byte[] Serialize(Chunk chunk, IReadOnlyList<ObjectInstance> anchoredObjects)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(anchoredObjects);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(Magic);
        writer.Write(2);
        writer.Write(chunk.Coord.X);
        writer.Write(chunk.Coord.Y);
        writer.Write(ChunkDimensions.Width);
        writer.Write(ChunkDimensions.Height);
        writer.Write(ChunkDimensions.CellCount);

        for (var localY = 0; localY < ChunkDimensions.Height; localY++)
        {
            for (var localX = 0; localX < ChunkDimensions.Width; localX++)
            {
                var cell = chunk.GetCell(localX, localY);
                writer.Write(cell.ForegroundTileId);
                writer.Write(cell.BackgroundWallId);
                writer.Write(cell.LiquidType);
                writer.Write(cell.LiquidAmount);
                writer.Write(cell.Variant);
                writer.Write(cell.Flags);
            }
        }

        writer.Write(anchoredObjects.Count);
        foreach (var instance in anchoredObjects)
        {
            writer.Write(instance.InstanceId);
            writer.Write(instance.ObjectDefId);
            writer.Write(instance.AnchorCoord.X);
            writer.Write(instance.AnchorCoord.Y);
            writer.Write((byte)instance.Direction);
            writer.Write(instance.StateFlags);
        }

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Deserializes a chunk payload and validates it against the expected chunk coordinate.
    /// </summary>
    /// <param name="data">The binary chunk payload.</param>
    /// <param name="expectedCoord">The chunk coordinate expected by the caller.</param>
    /// <returns>The deserialized chunk.</returns>
    /// <exception cref="InvalidDataException">Thrown when the payload is malformed, truncated, or mismatched.</exception>
    public Chunk Deserialize(byte[] data, ChunkCoord expectedCoord)
    {
        return DeserializePayload(data, expectedCoord).Chunk;
    }

    /// <summary>
    /// Deserializes a chunk payload, including any anchored object instances, and validates it against the expected chunk coordinate.
    /// </summary>
    /// <param name="data">The binary chunk payload.</param>
    /// <param name="expectedCoord">The chunk coordinate expected by the caller.</param>
    /// <returns>The deserialized chunk storage payload.</returns>
    /// <exception cref="InvalidDataException">Thrown when the payload is malformed, truncated, or mismatched.</exception>
    internal ChunkStoragePayload DeserializePayload(byte[] data, ChunkCoord expectedCoord)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            using var stream = new MemoryStream(data, writable: false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var magic = reader.ReadBytes(Magic.Length);
            if (magic.Length != Magic.Length || !magic.AsSpan().SequenceEqual(Magic))
            {
                throw new InvalidDataException("Chunk data has an invalid magic header.");
            }

            var chunkFormatVersion = reader.ReadInt32();
            if (chunkFormatVersion is not (1 or 2))
            {
                throw new InvalidDataException($"Unsupported chunk format version '{chunkFormatVersion}'.");
            }

            var chunkCoord = new ChunkCoord(reader.ReadInt32(), reader.ReadInt32());
            if (chunkCoord != expectedCoord)
            {
                throw new InvalidDataException(
                    $"Chunk data coord '{chunkCoord}' does not match expected coord '{expectedCoord}'.");
            }

            var chunkWidth = reader.ReadInt32();
            var chunkHeight = reader.ReadInt32();
            var cellCount = reader.ReadInt32();

            if (chunkWidth != ChunkDimensions.Width || chunkHeight != ChunkDimensions.Height || cellCount != ChunkDimensions.CellCount)
            {
                throw new InvalidDataException(
                    $"Chunk data dimensions ({chunkWidth}, {chunkHeight}, {cellCount}) do not match runtime chunk dimensions ({ChunkDimensions.Width}, {ChunkDimensions.Height}, {ChunkDimensions.CellCount}).");
            }

            var chunk = new Chunk(chunkCoord);

            for (var localY = 0; localY < ChunkDimensions.Height; localY++)
            {
                for (var localX = 0; localX < ChunkDimensions.Width; localX++)
                {
                    chunk.SetCell(localX, localY, new TileCell
                    {
                        ForegroundTileId = reader.ReadUInt16(),
                        BackgroundWallId = reader.ReadUInt16(),
                        LiquidType = reader.ReadByte(),
                        LiquidAmount = reader.ReadByte(),
                        Variant = reader.ReadUInt16(),
                        Flags = reader.ReadUInt16()
                    });
                }
            }

            var anchoredObjects = new List<ObjectInstance>();
            if (chunkFormatVersion >= 2)
            {
                var objectCount = reader.ReadInt32();
                if (objectCount < 0)
                {
                    throw new InvalidDataException("Chunk data contains a negative object count.");
                }

                for (var index = 0; index < objectCount; index++)
                {
                    anchoredObjects.Add(new ObjectInstance
                    {
                        InstanceId = reader.ReadInt32(),
                        ObjectDefId = reader.ReadInt32(),
                        AnchorCoord = new WorldTileCoord(reader.ReadInt32(), reader.ReadInt32()),
                        Direction = (Direction)reader.ReadByte(),
                        StateFlags = reader.ReadUInt16()
                    });
                }
            }

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException("Chunk data contains unexpected trailing bytes.");
            }

            return new ChunkStoragePayload(chunk, anchoredObjects);
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException("Chunk data is truncated.", exception);
        }
    }
}
