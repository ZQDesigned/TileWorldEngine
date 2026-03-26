using System;
using System.Collections.Generic;
using System.IO;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Objects;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Provides file-system access for world metadata and chunk payloads.
/// </summary>
public sealed class WorldStorage
{
    private readonly ChunkSerializer _chunkSerializer;
    private readonly WorldMetadataSerializer _worldMetadataSerializer;

    /// <summary>
    /// Creates storage services with the default metadata and chunk serializers.
    /// </summary>
    public WorldStorage()
        : this(new WorldMetadataSerializer(), new ChunkSerializer())
    {
    }

    /// <summary>
    /// Creates storage services with explicit serializer dependencies.
    /// </summary>
    /// <param name="worldMetadataSerializer">The serializer used for world metadata JSON.</param>
    /// <param name="chunkSerializer">The serializer used for binary chunk payloads.</param>
    public WorldStorage(WorldMetadataSerializer worldMetadataSerializer, ChunkSerializer chunkSerializer)
    {
        ArgumentNullException.ThrowIfNull(worldMetadataSerializer);
        ArgumentNullException.ThrowIfNull(chunkSerializer);

        _worldMetadataSerializer = worldMetadataSerializer;
        _chunkSerializer = chunkSerializer;
    }

    /// <summary>
    /// Returns <see langword="true"/> when metadata exists for the supplied world path.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <returns><see langword="true"/> when world metadata exists.</returns>
    public bool HasWorld(string worldPath)
    {
        return File.Exists(GetMetadataFilePath(worldPath));
    }

    /// <summary>
    /// Loads world metadata from the supplied world path.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <returns>The deserialized world metadata.</returns>
    public WorldMetadata LoadMetadata(string worldPath)
    {
        var metadataPath = GetMetadataFilePath(worldPath);
        return _worldMetadataSerializer.Deserialize(File.ReadAllText(metadataPath));
    }

    /// <summary>
    /// Saves world metadata to the supplied world path.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="metadata">The world metadata to serialize.</param>
    public void SaveMetadata(string worldPath, WorldMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        Directory.CreateDirectory(worldPath);
        File.WriteAllText(GetMetadataFilePath(worldPath), _worldMetadataSerializer.Serialize(metadata));
    }

    /// <summary>
    /// Attempts to load persisted chunk data for the supplied coordinate.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="coord">The chunk coordinate to load.</param>
    /// <returns>The loaded chunk when persisted data exists; otherwise <see langword="null"/>.</returns>
    public Chunk TryLoadChunk(string worldPath, ChunkCoord coord)
    {
        var chunkFilePath = GetChunkFilePath(worldPath, coord);
        return !File.Exists(chunkFilePath)
            ? null
            : _chunkSerializer.Deserialize(File.ReadAllBytes(chunkFilePath), coord);
    }

    /// <summary>
    /// Attempts to load persisted chunk data, including anchored objects, for the supplied coordinate.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="coord">The chunk coordinate to load.</param>
    /// <returns>The loaded payload when persisted data exists; otherwise <see langword="null"/>.</returns>
    internal ChunkStoragePayload TryLoadChunkPayload(string worldPath, ChunkCoord coord)
    {
        var chunkFilePath = GetChunkFilePath(worldPath, coord);
        return !File.Exists(chunkFilePath)
            ? null
            : _chunkSerializer.DeserializePayload(File.ReadAllBytes(chunkFilePath), coord);
    }

    /// <summary>
    /// Saves a chunk payload to the supplied world path.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="chunk">The chunk to serialize.</param>
    public void SaveChunk(string worldPath, Chunk chunk)
    {
        SaveChunk(worldPath, chunk, []);
    }

    /// <summary>
    /// Saves a chunk payload and its anchored objects to the supplied world path.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="chunk">The chunk to serialize.</param>
    /// <param name="anchoredObjects">The anchored object instances that should be stored with this chunk.</param>
    internal void SaveChunk(string worldPath, Chunk chunk, IReadOnlyList<ObjectInstance> anchoredObjects)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(anchoredObjects);

        var chunksDirectory = GetChunksDirectoryPath(worldPath);
        Directory.CreateDirectory(chunksDirectory);
        File.WriteAllBytes(GetChunkFilePath(worldPath, chunk.Coord), _chunkSerializer.Serialize(chunk, anchoredObjects));
    }

    /// <summary>
    /// Returns <see langword="true"/> when chunk data exists on disk for the supplied coordinate.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="coord">The chunk coordinate to inspect.</param>
    /// <returns><see langword="true"/> when persisted chunk data exists.</returns>
    public bool HasChunkData(string worldPath, ChunkCoord coord)
    {
        return File.Exists(GetChunkFilePath(worldPath, coord));
    }

    private static string GetMetadataFilePath(string worldPath)
    {
        return Path.Combine(worldPath, "world.json");
    }

    private static string GetChunksDirectoryPath(string worldPath)
    {
        return Path.Combine(worldPath, "chunks");
    }

    private static string GetChunkFilePath(string worldPath, ChunkCoord coord)
    {
        return Path.Combine(GetChunksDirectoryPath(worldPath), $"{coord.X}_{coord.Y}.chk");
    }
}
