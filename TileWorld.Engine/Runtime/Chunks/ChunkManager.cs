using System;
using System.Linq;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Chunks;

/// <summary>
/// Bridges in-memory chunks with on-disk chunk persistence.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="WorldRuntime"/> instead of taking a
/// direct dependency on chunk-loading and saving orchestration details.
/// </remarks>
internal sealed class ChunkManager
{
    private readonly WorldData _worldData;
    private readonly WorldStorage _worldStorage;
    private readonly string _worldPath;

    /// <summary>
    /// Creates a chunk manager for a single world path.
    /// </summary>
    /// <param name="worldData">The loaded world data that should receive resolved chunks.</param>
    /// <param name="worldStorage">The storage service used to load and save chunk payloads.</param>
    /// <param name="worldPath">The root path of the world on disk.</param>
    public ChunkManager(WorldData worldData, WorldStorage worldStorage, string worldPath)
    {
        ArgumentNullException.ThrowIfNull(worldData);
        ArgumentNullException.ThrowIfNull(worldStorage);
        ArgumentException.ThrowIfNullOrWhiteSpace(worldPath);

        _worldData = worldData;
        _worldStorage = worldStorage;
        _worldPath = worldPath;
    }

    /// <summary>
    /// Resolves a chunk from memory, storage, or by creating a new empty chunk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to resolve.</param>
    /// <returns>The loaded or newly created chunk.</returns>
    public Chunk GetOrLoadChunk(ChunkCoord coord)
    {
        if (_worldData.TryGetChunk(coord, out var loadedChunk))
        {
            return loadedChunk;
        }

        var chunk = _worldStorage.TryLoadChunk(_worldPath, coord) ?? new Chunk(coord);
        chunk.DirtyFlags |= ChunkDirtyFlags.RenderDirty;
        _worldData.SetChunk(chunk);
        return chunk;
    }

    /// <summary>
    /// Attempts to resolve a chunk that is already loaded in memory.
    /// </summary>
    /// <param name="coord">The chunk coordinate to resolve.</param>
    /// <param name="chunk">The loaded chunk when present.</param>
    /// <returns><see langword="true"/> when the chunk is already loaded in memory.</returns>
    public bool TryGetLoadedChunk(ChunkCoord coord, out Chunk chunk)
    {
        return _worldData.TryGetChunk(coord, out chunk!);
    }

    /// <summary>
    /// Persists all currently loaded chunks that are marked as save-dirty.
    /// </summary>
    /// <returns>The number of chunks written to storage.</returns>
    public int SaveDirtyChunks()
    {
        var dirtyChunks = _worldData
            .EnumerateLoadedChunks()
            .Where(chunk => (chunk.DirtyFlags & ChunkDirtyFlags.SaveDirty) != ChunkDirtyFlags.None)
            .ToArray();

        foreach (var chunk in dirtyChunks)
        {
            _worldStorage.SaveChunk(_worldPath, chunk);
            chunk.DirtyFlags &= ~ChunkDirtyFlags.SaveDirty;
        }

        return dirtyChunks.Length;
    }

    /// <summary>
    /// Persists every currently loaded chunk regardless of dirty state.
    /// </summary>
    /// <returns>The number of chunks written to storage.</returns>
    public int SaveAllLoadedChunks()
    {
        var savedChunkCount = 0;

        foreach (var chunk in _worldData.EnumerateLoadedChunks().ToArray())
        {
            _worldStorage.SaveChunk(_worldPath, chunk);
            chunk.DirtyFlags &= ~ChunkDirtyFlags.SaveDirty;
            savedChunkCount++;
        }

        return savedChunkCount;
    }
}
