using System;
using System.Collections.Generic;
using System.IO;
using TileWorld.Engine.Runtime.Entities;
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
    private readonly EntityPersistenceSerializer _entityPersistenceSerializer;
    private readonly RuntimeEntityBinarySerializer _runtimeEntityBinarySerializer;
    private readonly WorldMetadataSerializer _worldMetadataSerializer;

    /// <summary>
    /// Creates storage services with the default metadata and chunk serializers.
    /// </summary>
    public WorldStorage()
        : this(new WorldMetadataSerializer(), new ChunkSerializer(), new EntityPersistenceSerializer(), new RuntimeEntityBinarySerializer())
    {
    }

    /// <summary>
    /// Creates storage services with explicit serializer dependencies.
    /// </summary>
    /// <param name="worldMetadataSerializer">The serializer used for world metadata JSON.</param>
    /// <param name="chunkSerializer">The serializer used for binary chunk payloads.</param>
    public WorldStorage(WorldMetadataSerializer worldMetadataSerializer, ChunkSerializer chunkSerializer)
        : this(worldMetadataSerializer, chunkSerializer, new EntityPersistenceSerializer(), new RuntimeEntityBinarySerializer())
    {
    }

    /// <summary>
    /// Creates storage services with explicit serializer dependencies, including runtime-entity persistence.
    /// </summary>
    /// <param name="worldMetadataSerializer">The serializer used for world metadata JSON.</param>
    /// <param name="chunkSerializer">The serializer used for binary chunk payloads.</param>
    /// <param name="entityPersistenceSerializer">The serializer used for runtime-entity JSON payloads.</param>
    /// <param name="runtimeEntityBinarySerializer">The serializer used for persisted non-player runtime-entity binary payloads.</param>
    /// <remarks>
    /// Engine internal infrastructure API. Most callers should use the default constructor or inject a fully
    /// configured <see cref="WorldStorage"/> instance through <see cref="Runtime.WorldRuntimeOptions"/>.
    /// </remarks>
    internal WorldStorage(
        WorldMetadataSerializer worldMetadataSerializer,
        ChunkSerializer chunkSerializer,
        EntityPersistenceSerializer entityPersistenceSerializer,
        RuntimeEntityBinarySerializer runtimeEntityBinarySerializer)
    {
        ArgumentNullException.ThrowIfNull(worldMetadataSerializer);
        ArgumentNullException.ThrowIfNull(chunkSerializer);
        ArgumentNullException.ThrowIfNull(entityPersistenceSerializer);
        ArgumentNullException.ThrowIfNull(runtimeEntityBinarySerializer);

        _worldMetadataSerializer = worldMetadataSerializer;
        _chunkSerializer = chunkSerializer;
        _entityPersistenceSerializer = entityPersistenceSerializer;
        _runtimeEntityBinarySerializer = runtimeEntityBinarySerializer;
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

    /// <summary>
    /// Loads persisted player entities for the supplied world path.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <returns>The persisted player entities, or an empty collection when no player data exists.</returns>
    /// <remarks>
    /// Engine internal infrastructure API. Runtime bootstrap code should prefer <see cref="Runtime.WorldRuntime"/>
    /// instead of taking a direct dependency on world storage file layout.
    /// </remarks>
    internal IReadOnlyList<Entity> LoadPlayers(string worldPath)
    {
        var playerDataFilePath = GetPlayerDataFilePath(worldPath);
        return !File.Exists(playerDataFilePath)
            ? []
            : _entityPersistenceSerializer.Deserialize(File.ReadAllText(playerDataFilePath));
    }

    /// <summary>
    /// Saves persisted player entities to the supplied world path.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="players">The persisted player entities to write.</param>
    /// <remarks>
    /// Engine internal infrastructure API. Runtime bootstrap code should prefer <see cref="Runtime.WorldRuntime"/>
    /// instead of taking a direct dependency on world storage file layout.
    /// </remarks>
    internal void SavePlayers(string worldPath, IReadOnlyList<Entity> players)
    {
        SaveEntityFile(GetPlayerDataDirectoryPath(worldPath), GetPlayerDataFilePath(worldPath), players);
    }

    /// <summary>
    /// Loads persisted non-player runtime entities for the supplied world path.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <returns>The persisted non-player entities, or an empty collection when no entity data exists.</returns>
    /// <remarks>
    /// Engine internal infrastructure API. Runtime bootstrap code should prefer <see cref="Runtime.WorldRuntime"/>
    /// instead of taking a direct dependency on world storage file layout.
    /// </remarks>
    internal IReadOnlyList<Entity> LoadRuntimeEntities(string worldPath)
    {
        var entityFilePath = GetRuntimeEntitiesBinaryFilePath(worldPath);
        if (File.Exists(entityFilePath))
        {
            return _runtimeEntityBinarySerializer.Deserialize(File.ReadAllBytes(entityFilePath));
        }

        var legacyJsonFilePath = GetRuntimeEntitiesLegacyJsonFilePath(worldPath);
        return File.Exists(legacyJsonFilePath)
            ? _entityPersistenceSerializer.Deserialize(File.ReadAllText(legacyJsonFilePath))
            : [];
    }

    /// <summary>
    /// Saves persisted non-player runtime entities to the supplied world path.
    /// </summary>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="entities">The persisted non-player entities to write.</param>
    /// <remarks>
    /// Engine internal infrastructure API. Runtime bootstrap code should prefer <see cref="Runtime.WorldRuntime"/>
    /// instead of taking a direct dependency on world storage file layout.
    /// </remarks>
    internal void SaveRuntimeEntities(string worldPath, IReadOnlyList<Entity> entities)
    {
        SaveBinaryEntityFile(GetRuntimeEntitiesDirectoryPath(worldPath), GetRuntimeEntitiesBinaryFilePath(worldPath), entities);
        var legacyJsonFilePath = GetRuntimeEntitiesLegacyJsonFilePath(worldPath);
        if (File.Exists(legacyJsonFilePath))
        {
            File.Delete(legacyJsonFilePath);
        }
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

    private void SaveEntityFile(string directoryPath, string filePath, IReadOnlyList<Entity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (entities.Count == 0)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return;
        }

        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(filePath, _entityPersistenceSerializer.Serialize(entities));
    }

    private void SaveBinaryEntityFile(string directoryPath, string filePath, IReadOnlyList<Entity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (entities.Count == 0)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return;
        }

        Directory.CreateDirectory(directoryPath);
        File.WriteAllBytes(filePath, _runtimeEntityBinarySerializer.Serialize(entities));
    }

    private static string GetPlayerDataDirectoryPath(string worldPath)
    {
        return Path.Combine(worldPath, "playerdata");
    }

    private static string GetPlayerDataFilePath(string worldPath)
    {
        return Path.Combine(GetPlayerDataDirectoryPath(worldPath), "players.json");
    }

    private static string GetRuntimeEntitiesDirectoryPath(string worldPath)
    {
        return Path.Combine(worldPath, "entities");
    }

    private static string GetRuntimeEntitiesFilePath(string worldPath)
    {
        return GetRuntimeEntitiesBinaryFilePath(worldPath);
    }

    private static string GetRuntimeEntitiesBinaryFilePath(string worldPath)
    {
        return Path.Combine(GetRuntimeEntitiesDirectoryPath(worldPath), "entities.bin");
    }

    private static string GetRuntimeEntitiesLegacyJsonFilePath(string worldPath)
    {
        return Path.Combine(GetRuntimeEntitiesDirectoryPath(worldPath), "entities.json");
    }
}
