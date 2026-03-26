using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Objects;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Generation;

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
    private readonly ContentRegistry _contentRegistry;
    private readonly WorldEventBus _eventBus;
    private readonly IWorldGenerator _generator;
    private ObjectManager _objectManager;
    private readonly int _prefetchRadiusInChunks;
    private readonly ChunkStreamingCoordinator _streamingCoordinator;
    private readonly WorldData _worldData;
    private readonly WorldStorage _worldStorage;
    private readonly string _worldPath;
    private bool _isShutdown;

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
        : this(worldData, worldStorage, worldPath, contentRegistry: null, generator: null, activeRadiusInChunks, eventBus)
    {
    }

    /// <summary>
    /// Creates a chunk manager for a single world path with optional chunk generation support.
    /// </summary>
    /// <param name="worldData">The loaded world data that should receive resolved chunks.</param>
    /// <param name="worldStorage">The storage service used to load and save chunk payloads.</param>
    /// <param name="worldPath">The root path of the world on disk.</param>
    /// <param name="contentRegistry">The content registry used by generated chunks.</param>
    /// <param name="generator">The optional generator used when chunk data is not yet persisted.</param>
    /// <param name="activeRadiusInChunks">The chunk radius that should remain active around the current center.</param>
    /// <param name="eventBus">The event bus used to publish chunk lifecycle events.</param>
    public ChunkManager(
        WorldData worldData,
        WorldStorage worldStorage,
        string worldPath,
        ContentRegistry contentRegistry,
        IWorldGenerator generator,
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
        _prefetchRadiusInChunks = _activeRadiusInChunks + 1;
        _contentRegistry = contentRegistry;
        _generator = generator;
        _eventBus = eventBus ?? new WorldEventBus();
        _streamingCoordinator = new ChunkStreamingCoordinator(ResolveChunkOnBackgroundThread);
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
        FlushPrefetchedChunks();

        if (_worldData.TryGetChunk(coord, out var loadedChunk))
        {
            return new ChunkLoadResult(loadedChunk, ChunkLoadSource.Memory);
        }

        var resolvedChunk = ResolveChunk(coord);
        var chunk = IntegrateResolvedChunk(resolvedChunk);
        return new ChunkLoadResult(chunk, resolvedChunk.Source);
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
    /// Updates the active chunk set around a world-tile area.
    /// </summary>
    /// <param name="tileBounds">The world-tile area that should remain active.</param>
    /// <param name="activePaddingInChunks">The number of additional chunk rings that should stay synchronously active.</param>
    public void EnsureActiveForTileArea(RectI tileBounds, int activePaddingInChunks = 0)
    {
        UpdateActiveSetForTileArea(tileBounds, activePaddingInChunks);
    }

    /// <summary>
    /// Updates the active chunk set around a world-tile center.
    /// </summary>
    /// <param name="center">The center world-tile coordinate.</param>
    public void UpdateActiveSet(WorldTileCoord center)
    {
        UpdateActiveSetForTileArea(new RectI(center.X, center.Y, 1, 1), _activeRadiusInChunks);
    }

    private void UpdateActiveSetForTileArea(RectI tileBounds, int activePaddingInChunks)
    {
        FlushPrefetchedChunks();
        var padding = Math.Max(0, activePaddingInChunks);
        var minChunk = WorldCoordinateConverter.ToChunkCoord(new WorldTileCoord(tileBounds.Left, tileBounds.Top));
        var maxChunk = WorldCoordinateConverter.ToChunkCoord(new WorldTileCoord(GetInclusiveRight(tileBounds), GetInclusiveBottom(tileBounds)));
        var activeMinChunkX = minChunk.X - padding;
        var activeMaxChunkX = maxChunk.X + padding;
        var activeMinChunkY = minChunk.Y - padding;
        var activeMaxChunkY = maxChunk.Y + padding;
        var desiredActiveChunks = new HashSet<ChunkCoord>();

        for (var chunkY = activeMinChunkY; chunkY <= activeMaxChunkY; chunkY++)
        {
            for (var chunkX = activeMinChunkX; chunkX <= activeMaxChunkX; chunkX++)
            {
                var coord = new ChunkCoord(chunkX, chunkY);
                if (!WorldVerticalBoundsUtility.DoesChunkIntersectBounds(_worldData.Metadata, coord))
                {
                    continue;
                }

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

        var prefetchMinChunkX = activeMinChunkX - 1;
        var prefetchMaxChunkX = activeMaxChunkX + 1;
        var prefetchMinChunkY = activeMinChunkY - 1;
        var prefetchMaxChunkY = activeMaxChunkY + 1;
        QueueOuterRing(
            activeMinChunkX,
            activeMaxChunkX,
            activeMinChunkY,
            activeMaxChunkY,
            prefetchMinChunkX,
            prefetchMaxChunkX,
            prefetchMinChunkY,
            prefetchMaxChunkY,
            desiredActiveChunks);
        UnloadOutsideChunkRange(prefetchMinChunkX, prefetchMaxChunkX, prefetchMinChunkY, prefetchMaxChunkY);
    }

    /// <summary>
    /// Unloads loaded chunks that fall outside the active radius around a world-tile center.
    /// </summary>
    /// <param name="center">The center world-tile coordinate.</param>
    public void UnloadFarChunks(WorldTileCoord center)
    {
        UnloadFarChunks(center, _activeRadiusInChunks);
    }

    /// <summary>
    /// Integrates completed background chunk prefetch work into the live world.
    /// </summary>
    public void Update()
    {
        FlushPrefetchedChunks();
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
        FlushPrefetchedChunks();
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
        FlushPrefetchedChunks();
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

    /// <summary>
    /// Shuts down background prefetch work after integrating any completed chunk payloads.
    /// </summary>
    public void Shutdown()
    {
        if (_isShutdown)
        {
            return;
        }

        FlushPrefetchedChunks();
        _streamingCoordinator.Shutdown();
        _isShutdown = true;
    }

    private void SaveChunk(Chunk chunk)
    {
        var anchoredObjects = _objectManager?.GetPersistedObjectsForChunk(chunk.Coord) ?? [];
        _worldStorage.SaveChunk(_worldPath, chunk, anchoredObjects);
    }

    private void UnloadFarChunks(WorldTileCoord center, int radiusInChunks)
    {
        var centerChunk = WorldCoordinateConverter.ToChunkCoord(center);
        var unloadCandidates = _worldData
            .EnumerateLoadedChunks()
            .Select(chunk => chunk.Coord)
            .Where(coord =>
                Math.Abs(coord.X - centerChunk.X) > radiusInChunks ||
                Math.Abs(coord.Y - centerChunk.Y) > radiusInChunks)
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

    private void UnloadOutsideChunkRange(int minChunkX, int maxChunkX, int minChunkY, int maxChunkY)
    {
        var unloadCandidates = _worldData
            .EnumerateLoadedChunks()
            .Select(chunk => chunk.Coord)
            .Where(coord =>
                coord.X < minChunkX ||
                coord.X > maxChunkX ||
                coord.Y < minChunkY ||
                coord.Y > maxChunkY)
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

    private void QueueOuterRing(
        int activeMinChunkX,
        int activeMaxChunkX,
        int activeMinChunkY,
        int activeMaxChunkY,
        int prefetchMinChunkX,
        int prefetchMaxChunkX,
        int prefetchMinChunkY,
        int prefetchMaxChunkY,
        IReadOnlySet<ChunkCoord> desiredActiveChunks)
    {
        if (_isShutdown)
        {
            return;
        }

        for (var chunkY = prefetchMinChunkY; chunkY <= prefetchMaxChunkY; chunkY++)
        {
            for (var chunkX = prefetchMinChunkX; chunkX <= prefetchMaxChunkX; chunkX++)
            {
                if (chunkX >= activeMinChunkX &&
                    chunkX <= activeMaxChunkX &&
                    chunkY >= activeMinChunkY &&
                    chunkY <= activeMaxChunkY)
                {
                    continue;
                }

                var coord = new ChunkCoord(chunkX, chunkY);
                if (!WorldVerticalBoundsUtility.DoesChunkIntersectBounds(_worldData.Metadata, coord) ||
                    desiredActiveChunks.Contains(coord) ||
                    _worldData.HasChunk(coord))
                {
                    continue;
                }

                if (_streamingCoordinator.Queue(coord))
                {
                    _eventBus.Publish(new ChunkQueuedEvent(coord));
                }
            }
        }
    }

    private void FlushPrefetchedChunks()
    {
        if (_isShutdown)
        {
            return;
        }

        foreach (var prefetchedChunk in _streamingCoordinator.DrainCompleted())
        {
            IntegrateResolvedChunk(prefetchedChunk);
        }
    }

    private Chunk IntegrateResolvedChunk(ChunkStreamingCoordinator.PrefetchedChunkResult resolvedChunk)
    {
        if (_worldData.TryGetChunk(resolvedChunk.Coord, out var existingChunk))
        {
            return existingChunk;
        }

        var chunk = resolvedChunk.Payload.Chunk;
        chunk.State = ChunkState.Loaded;
        if (resolvedChunk.Source != ChunkLoadSource.Memory)
        {
            chunk.DirtyFlags |= ChunkDirtyFlags.RenderDirty;
        }

        if (resolvedChunk.Source == ChunkLoadSource.Generated)
        {
            chunk.DirtyFlags |= ChunkDirtyFlags.SaveDirty;
        }

        _worldData.SetChunk(chunk);
        foreach (var anchoredObject in resolvedChunk.Payload.AnchoredObjects)
        {
            _objectManager?.RegisterLoadedObject(anchoredObject);
        }

        _eventBus.Publish(new ChunkLoadedEvent(resolvedChunk.Coord, resolvedChunk.Source));
        return chunk;
    }

    private ChunkStreamingCoordinator.PrefetchedChunkResult ResolveChunk(ChunkCoord coord)
    {
        return ResolveChunkOnBackgroundThread(coord);
    }

    private ChunkStreamingCoordinator.PrefetchedChunkResult ResolveChunkOnBackgroundThread(ChunkCoord coord)
    {
        if (!WorldVerticalBoundsUtility.DoesChunkIntersectBounds(_worldData.Metadata, coord))
        {
            return CreateEmptyResult(coord);
        }

        var payload = _worldStorage.TryLoadChunkPayload(_worldPath, coord, _worldData.Metadata);
        if (payload is not null)
        {
            payload.Chunk.State = ChunkState.Loading;
            return new ChunkStreamingCoordinator.PrefetchedChunkResult(coord, payload, ChunkLoadSource.Disk);
        }

        if (_generator is not null && _contentRegistry is not null)
        {
            var generatedChunk = _generator.GenerateChunk(CreateGenerationContext(), coord).Chunk;
            generatedChunk.State = ChunkState.Loading;
            return new ChunkStreamingCoordinator.PrefetchedChunkResult(
                coord,
                new ChunkStoragePayload(generatedChunk, []),
                ChunkLoadSource.Generated);
        }

        return CreateEmptyResult(coord);
    }

    private WorldGenerationContext CreateGenerationContext()
    {
        return new WorldGenerationContext
        {
            Metadata = _worldData.Metadata,
            ContentRegistry = _contentRegistry
        };
    }

    private static ChunkStreamingCoordinator.PrefetchedChunkResult CreateEmptyResult(ChunkCoord coord)
    {
        var emptyChunk = new Chunk(coord)
        {
            State = ChunkState.Loading
        };

        return new ChunkStreamingCoordinator.PrefetchedChunkResult(
            coord,
            new ChunkStoragePayload(emptyChunk, []),
            ChunkLoadSource.EmptyCreated);
    }

    private static int GetInclusiveRight(RectI bounds)
    {
        return bounds.Width == 0 ? bounds.Left : bounds.Right - 1;
    }

    private static int GetInclusiveBottom(RectI bounds)
    {
        return bounds.Height == 0 ? bounds.Top : bounds.Bottom - 1;
    }
}
