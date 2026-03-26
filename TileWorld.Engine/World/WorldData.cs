using System;
using System.Collections.Generic;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World;

/// <summary>
/// Owns loaded chunks and metadata for a single world instance.
/// </summary>
public sealed class WorldData
{
    private readonly Dictionary<ChunkCoord, Chunk> _chunks = new();

    /// <summary>
    /// Creates a world-data container for the supplied metadata.
    /// </summary>
    /// <param name="metadata">The metadata associated with the world instance.</param>
    public WorldData(WorldMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        Metadata = metadata;
    }

    /// <summary>
    /// Gets the persistent metadata associated with this world.
    /// </summary>
    public WorldMetadata Metadata { get; private set; }

    /// <summary>
    /// Gets the number of chunks currently loaded in memory.
    /// </summary>
    public int LoadedChunkCount => _chunks.Count;

    /// <summary>
    /// Attempts to resolve a loaded chunk by coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate to resolve.</param>
    /// <param name="chunk">The loaded chunk when present.</param>
    /// <returns><see langword="true"/> when a chunk is already loaded at the supplied coordinate.</returns>
    public bool TryGetChunk(ChunkCoord coord, out Chunk chunk)
    {
        return _chunks.TryGetValue(coord, out chunk!);
    }

    /// <summary>
    /// Resolves a chunk from memory or creates a new empty loaded chunk when one is not present.
    /// </summary>
    /// <param name="coord">The chunk coordinate to resolve.</param>
    /// <returns>The existing or newly created chunk.</returns>
    public Chunk GetOrCreateChunk(ChunkCoord coord)
    {
        if (_chunks.TryGetValue(coord, out var existingChunk))
        {
            return existingChunk;
        }

        var chunk = new Chunk(coord);
        _chunks.Add(coord, chunk);

        return chunk;
    }

    /// <summary>
    /// Stores or replaces a loaded chunk at its coordinate.
    /// </summary>
    /// <param name="chunk">The chunk to store.</param>
    public void SetChunk(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        _chunks[chunk.Coord] = chunk;
    }

    /// <summary>
    /// Returns whether a chunk is currently loaded at the supplied coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate to inspect.</param>
    /// <returns><see langword="true"/> when the chunk is loaded.</returns>
    public bool HasChunk(ChunkCoord coord)
    {
        return _chunks.ContainsKey(coord);
    }

    /// <summary>
    /// Removes a loaded chunk from memory when present.
    /// </summary>
    /// <param name="coord">The chunk coordinate to remove.</param>
    /// <returns><see langword="true"/> when a loaded chunk was removed.</returns>
    public bool RemoveChunk(ChunkCoord coord)
    {
        return _chunks.Remove(coord);
    }

    /// <summary>
    /// Enumerates all chunks currently loaded in memory.
    /// </summary>
    /// <returns>The currently loaded chunks.</returns>
    public IEnumerable<Chunk> EnumerateLoadedChunks()
    {
        return _chunks.Values;
    }

    /// <summary>
    /// Replaces the persistent metadata associated with this world.
    /// </summary>
    /// <param name="metadata">The metadata to store.</param>
    public void UpdateMetadata(WorldMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        Metadata = metadata;
    }
}
