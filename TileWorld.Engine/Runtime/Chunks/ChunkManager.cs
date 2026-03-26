using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Objects;
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
    private readonly HashSet<ChunkCoord> _activeChunks = [];
    private readonly int _activeRadiusInChunks;
    private readonly WorldEventBus _eventBus;
    private ObjectManager _objectManager;
    private readonly WorldData _worldData;
    private readonly WorldStorage _worldStorage;
    private readonly string _worldPath;

    /// <summary>
    /// Creates a chunk manager for a single world path.
    /// </summary>
    /// <param name="worldData">The loaded world data that should receive resolved chunks.</param>
    /// <param name="worldStorage">The storage service used to load and save chunk payloads.</param>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="activeRadiusInChunks">The chunk radius that should remain active around the current center.</param>
    /// <param name="eventBus">The event bus used to publish chunk lifecycle events.</param>
    public ChunkManager(
        WorldData worldData,
        WorldStorage worldStorage,
        string worldPath,
        int activeRadiusInChunks = 2,
        WorldEventBus eventBus = null)
    {
        ArgumentNullException.ThrowIfNull(worldData);
        ArgumentNullException.ThrowIfNull(worldStorage);
        ArgumentException.ThrowIfNullOrWhiteSpace(worldPath);

        _worldData = worldData;
        _worldStorage = worldStorage;
        _worldPath = worldPath;
        _activeRadiusInChunks = Math.Max(1, activeRadiusInChunks);
        _eventBus = eventBus ?? new WorldEventBus();
    }

    /// <summary>
    /// Resolves a chunk from memory, storage, or by creating a new empty chunk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to resolve.</param>
    /// <returns>The loaded or newly created chunk.</returns>
    public Chunk GetOrLoadChunk(ChunkCoord coord)
    {
        return GetOrLoadChunkDetailed(coord).Chunk;
    }

    /// <summary>
    /// Resolves a chunk from memory, storage, or by creating a new empty chunk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to resolve.</param>
    /// <returns>The detailed chunk load result.</returns>
    public ChunkLoadResult GetOrLoadChunkDetailed(ChunkCoord coord)
    {
        if (_worldData.TryGetChunk(coord, out var loadedChunk))
        {
            return new ChunkLoadResult(loadedChunk, WasLoadedFromMemory: true, WasLoadedFromDisk: false, WasCreatedNew: false);
        }

        var payload = _worldStorage.TryLoadChunkPayload(_worldPath, coord);
        var chunk = payload?.Chunk ?? new Chunk(coord);
        chunk.DirtyFlags |= ChunkDirtyFlags.RenderDirty;
        chunk.State = ChunkState.Loaded;
        _worldData.SetChunk(chunk);
        if (payload is not null)
        {
            foreach (var anchoredObject in payload.AnchoredObjects)
            {
                _objectManager?.RegisterLoadedObject(anchoredObject);
            }
        }

        _eventBus.Publish(new ChunkLoadedEvent(coord, payload is not null, payload is null));
        return new ChunkLoadResult(chunk, WasLoadedFromMemory: false, WasLoadedFromDisk: payload is not null, WasCreatedNew: payload is null);
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
    /// Updates the active chunk set around a world-tile center.
    /// </summary>
    /// <param name="center">The center world-tile coordinate.</param>
    public void EnsureActiveAround(WorldTileCoord center)
    {
        UpdateActiveSet(center);
    }

    /// <summary>
    /// Updates the active chunk set around a world-tile center.
    /// </summary>
    /// <param name="center">The center world-tile coordinate.</param>
    public void UpdateActiveSet(WorldTileCoord center)
    {
        var centerChunk = WorldCoordinateConverter.ToChunkCoord(center);
        var desiredActiveChunks = new HashSet<ChunkCoord>();

        for (var offsetY = -_activeRadiusInChunks; offsetY <= _activeRadiusInChunks; offsetY++)
        {
            for (var offsetX = -_activeRadiusInChunks; offsetX <= _activeRadiusInChunks; offsetX++)
            {
                var coord = centerChunk.Offset(offsetX, offsetY);
                desiredActiveChunks.Add(coord);
                var chunk = GetOrLoadChunk(coord);
                if (chunk.State != ChunkState.Active)
                {
                    chunk.State = ChunkState.Active;
                    _eventBus.Publish(new ChunkActivatedEvent(coord));
                }
            }
        }

        foreach (var staleCoord in _activeChunks.Except(desiredActiveChunks).ToArray())
        {
            if (!_worldData.TryGetChunk(staleCoord, out var chunk))
            {
                continue;
            }

            chunk.State = ChunkState.Inactive;
            _eventBus.Publish(new ChunkDeactivatedEvent(staleCoord));
        }

        _activeChunks.Clear();
        foreach (var coord in desiredActiveChunks)
        {
            _activeChunks.Add(coord);
        }

        UnloadFarChunks(center);
    }

    /// <summary>
    /// Unloads loaded chunks that fall outside the active radius around a world-tile center.
    /// </summary>
    /// <param name="center">The center world-tile coordinate.</param>
    public void UnloadFarChunks(WorldTileCoord center)
    {
        var centerChunk = WorldCoordinateConverter.ToChunkCoord(center);
        var unloadCandidates = _worldData
            .EnumerateLoadedChunks()
            .Select(chunk => chunk.Coord)
            .Where(coord =>
                Math.Abs(coord.X - centerChunk.X) > _activeRadiusInChunks ||
                Math.Abs(coord.Y - centerChunk.Y) > _activeRadiusInChunks)
            .ToArray();

        foreach (var coord in unloadCandidates)
        {
            if (!_worldData.TryGetChunk(coord, out var chunk))
            {
                continue;
            }

            _eventBus.Publish(new ChunkUnloadingEvent(coord));

            if ((chunk.DirtyFlags & ChunkDirtyFlags.SaveDirty) != ChunkDirtyFlags.None)
            {
                SaveChunk(chunk);
                chunk.DirtyFlags &= ~ChunkDirtyFlags.SaveDirty;
            }

            _objectManager?.RemoveObjectsAnchoredInChunk(coord);
            chunk.State = ChunkState.Unloaded;
            _worldData.RemoveChunk(coord);
            _activeChunks.Remove(coord);
        }
    }

    /// <summary>
    /// Enumerates the currently active chunk coordinates.
    /// </summary>
    /// <returns>The currently active chunk coordinates.</returns>
    public IEnumerable<ChunkCoord> GetActiveChunks()
    {
        return _activeChunks;
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
            SaveChunk(chunk);
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
            SaveChunk(chunk);
            chunk.DirtyFlags &= ~ChunkDirtyFlags.SaveDirty;
            savedChunkCount++;
        }

        return savedChunkCount;
    }

    public void AttachObjectManager(ObjectManager objectManager)
    {
        _objectManager = objectManager;
    }

    private void SaveChunk(Chunk chunk)
    {
        var anchoredObjects = _objectManager?.GetPersistedObjectsForChunk(chunk.Coord) ?? [];
        _worldStorage.SaveChunk(_worldPath, chunk, anchoredObjects);
    }
}
