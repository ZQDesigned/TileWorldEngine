using System;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Runtime.AutoTile;
using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Edits;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.Runtime.Tracking;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime;

/// <summary>
/// Composes world data, editing rules, persistence hooks, and advanced tooling support behind a single gameplay-facing facade.
/// </summary>
/// <remarks>
/// Prefer this type as the stable entry point for gameplay code, editor tooling, and hosts. Lower-level runtime
/// services are kept internal so external callers do not accidentally couple themselves to transient engine plumbing.
/// </remarks>
public sealed class WorldRuntime
{
    private bool _isInitialized;
    private TimeSpan? _lastMutationTime;
    private TimeSpan _lastObservedUpdateTime;
    private TimeSpan _lastSaveTime;
    private bool _pendingMutationObserved;

    /// <summary>
    /// Creates an in-memory world runtime without persistence options.
    /// </summary>
    /// <param name="worldData">The world data to own and mutate.</param>
    /// <param name="contentRegistry">The content registry used to resolve tile definitions.</param>
    public WorldRuntime(WorldData worldData, ContentRegistry contentRegistry)
        : this(worldData, contentRegistry, null)
    {
    }

    /// <summary>
    /// Creates a world runtime with optional persistence and auto-save behavior.
    /// </summary>
    /// <param name="worldData">The world data to own and mutate.</param>
    /// <param name="contentRegistry">The content registry used to resolve tile definitions.</param>
    /// <param name="options">Optional persistence and auto-save configuration.</param>
    public WorldRuntime(WorldData worldData, ContentRegistry contentRegistry, WorldRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(worldData);
        ArgumentNullException.ThrowIfNull(contentRegistry);

        WorldData = worldData;
        ContentRegistry = contentRegistry;
        Options = options ?? new WorldRuntimeOptions();
        Storage = CreateWorldStorage(Options);
        ChunkManager = CreateChunkManager(worldData, Storage, Options);
        QueryService = new WorldQueryService(worldData, contentRegistry, ChunkManager);
        DirtyTracker = new DirtyTracker(worldData);
        EventBus = new WorldEventBus();
        EventBus.Subscribe<TileChangedEvent>(_ => _pendingMutationObserved = true);
        AutoTileSystem = new AutoTileSystem(worldData, QueryService);
        TileEditService = new TileEditService(worldData, contentRegistry, QueryService, DirtyTracker, EventBus, AutoTileSystem, ChunkManager);
    }

    /// <summary>
    /// Gets the mutable world data owned by this runtime.
    /// </summary>
    public WorldData WorldData { get; }

    /// <summary>
    /// Gets the content registry used to resolve tile definitions referenced by the runtime.
    /// </summary>
    public ContentRegistry ContentRegistry { get; }

    /// <summary>
    /// Gets the runtime options that control persistence and auto-save behavior.
    /// </summary>
    public WorldRuntimeOptions Options { get; }

    /// <summary>
    /// Gets the storage backend when persistence is enabled.
    /// </summary>
    /// <remarks>
    /// This property is available for hosting and diagnostics scenarios. Gameplay code should usually prefer
    /// higher-level methods such as <see cref="SaveWorld"/> and <see cref="EnsureChunkLoaded"/>.
    /// </remarks>
    public WorldStorage Storage { get; }

    internal ChunkManager ChunkManager { get; }

    /// <summary>
    /// Gets a value indicating whether this runtime is currently backed by persistent world storage.
    /// </summary>
    public bool IsPersistenceEnabled => ChunkManager is not null;

    internal WorldQueryService QueryService { get; }

    internal DirtyTracker DirtyTracker { get; }

    internal WorldEventBus EventBus { get; }

    internal AutoTileSystem AutoTileSystem { get; }

    internal TileEditService TileEditService { get; }

    /// <summary>
    /// Transitions the runtime into the initialized state.
    /// </summary>
    public void Initialize()
    {
        _isInitialized = true;
    }

    /// <summary>
    /// Advances runtime bookkeeping for the current frame, including auto-save checks.
    /// </summary>
    /// <param name="frameTime">The frame timing snapshot supplied by the active host.</param>
    public void Update(FrameTime frameTime)
    {
        if (!_isInitialized)
        {
            return;
        }

        _lastObservedUpdateTime = frameTime.TotalTime;
        if (_pendingMutationObserved)
        {
            _lastMutationTime = frameTime.TotalTime;
            _pendingMutationObserved = false;
        }

        UpdateAutoSave(frameTime);
    }

    /// <summary>
    /// Shuts down the runtime and performs final persistence when enabled.
    /// </summary>
    public void Shutdown()
    {
        if (_isInitialized && IsPersistenceEnabled && Options.SaveOnShutdown)
        {
            SaveWorld();
        }

        _isInitialized = false;
    }

    /// <summary>
    /// Resolves a cell at the supplied world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <param name="options">Optional query behavior overrides.</param>
    /// <returns>The resolved cell, or <see cref="TileCell.Empty"/> when no cell data is available.</returns>
    public TileCell GetCell(WorldTileCoord coord, QueryOptions options = null)
    {
        return QueryService.GetCell(coord, options);
    }

    /// <summary>
    /// Attempts to resolve a cell at the supplied world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <param name="cell">The resolved cell when available.</param>
    /// <param name="options">Optional query behavior overrides.</param>
    /// <returns><see langword="true"/> when the cell was resolved.</returns>
    public bool TryGetCell(WorldTileCoord coord, out TileCell cell, QueryOptions options = null)
    {
        return QueryService.TryGetCell(coord, out cell, options);
    }

    /// <summary>
    /// Returns whether the foreground tile at the supplied coordinate is solid.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns><see langword="true"/> when the foreground tile is solid.</returns>
    public bool IsSolid(WorldTileCoord coord)
    {
        return QueryService.IsSolid(coord);
    }

    /// <summary>
    /// Places a foreground tile using placement semantics and validation rules.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="tileId">The non-air tile identifier to place.</param>
    /// <param name="context">Placement metadata and behavior flags.</param>
    /// <returns>The outcome of the placement attempt.</returns>
    public TileEditResult PlaceTile(WorldTileCoord coord, ushort tileId, TilePlacementContext context)
    {
        return TileEditService.PlaceTile(coord, tileId, context);
    }

    /// <summary>
    /// Breaks the foreground tile at the supplied coordinate using break semantics and validation rules.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="context">Break metadata and behavior flags.</param>
    /// <returns>The outcome of the break attempt.</returns>
    public TileEditResult BreakTile(WorldTileCoord coord, TileBreakContext context)
    {
        return TileEditService.BreakTile(coord, context);
    }

    /// <summary>
    /// Writes a non-air foreground tile directly to the world without placement validation.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="tileId">The non-air tile identifier to write.</param>
    /// <returns>The outcome of the write operation.</returns>
    public TileEditResult SetForegroundTile(WorldTileCoord coord, ushort tileId)
    {
        return TileEditService.SetForegroundTile(coord, tileId);
    }

    /// <summary>
    /// Removes the foreground tile at the supplied coordinate without break validation.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <returns>The outcome of the removal operation.</returns>
    public TileEditResult RemoveForegroundTile(WorldTileCoord coord)
    {
        return TileEditService.RemoveForegroundTile(coord);
    }

    /// <summary>
    /// Ensures that the chunk at the supplied coordinate is available in memory.
    /// </summary>
    /// <param name="coord">The chunk coordinate to load or create.</param>
    /// <returns>The loaded or newly created chunk.</returns>
    /// <remarks>
    /// This method is intended for hosts, tooling, and controlled world bootstrap flows. Gameplay logic should
    /// usually access world state via tile-level APIs such as <see cref="GetCell"/>, <see cref="PlaceTile"/>,
    /// and <see cref="BreakTile"/>.
    /// </remarks>
    public World.Chunks.Chunk EnsureChunkLoaded(ChunkCoord coord)
    {
        return ChunkManager is not null
            ? ChunkManager.GetOrLoadChunk(coord)
            : WorldData.GetOrCreateChunk(coord);
    }

    /// <summary>
    /// Attempts to resolve a chunk that is already loaded in memory.
    /// </summary>
    /// <param name="coord">The chunk coordinate to inspect.</param>
    /// <param name="chunk">The loaded chunk when present.</param>
    /// <returns><see langword="true"/> when the chunk is already loaded.</returns>
    public bool TryGetLoadedChunk(ChunkCoord coord, out World.Chunks.Chunk chunk)
    {
        return WorldData.TryGetChunk(coord, out chunk!);
    }

    /// <summary>
    /// Subscribes to a runtime event stream.
    /// </summary>
    /// <typeparam name="TEvent">The event payload type to observe.</typeparam>
    /// <param name="handler">The handler that should receive published events.</param>
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        EventBus.Subscribe(handler);
    }

    /// <summary>
    /// Unsubscribes a previously registered runtime event handler.
    /// </summary>
    /// <typeparam name="TEvent">The event payload type to stop observing.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        EventBus.Unsubscribe(handler);
    }

    /// <summary>
    /// Persists metadata and all save-dirty chunks for the current world.
    /// </summary>
    /// <returns>The number of chunk payloads written to storage.</returns>
    public int SaveWorld()
    {
        if (!IsPersistenceEnabled)
        {
            return 0;
        }

        Storage.SaveMetadata(Options.WorldPath, WorldData.Metadata);
        var savedChunkCount = ChunkManager.SaveDirtyChunks();
        _lastSaveTime = _lastObservedUpdateTime;

        if (GetDirtySaveChunkCount() == 0)
        {
            _lastMutationTime = null;
        }

        return savedChunkCount;
    }

    private static WorldStorage CreateWorldStorage(WorldRuntimeOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.WorldPath)
            ? options.WorldStorage ?? new WorldStorage()
            : null;
    }

    private static ChunkManager CreateChunkManager(WorldData worldData, WorldStorage storage, WorldRuntimeOptions options)
    {
        return storage is not null
            ? new ChunkManager(worldData, storage, options.WorldPath)
            : null;
    }

    private void UpdateAutoSave(FrameTime frameTime)
    {
        if (!IsPersistenceEnabled || !Options.EnableAutoSave)
        {
            return;
        }

        var dirtyChunkCount = GetDirtySaveChunkCount();
        if (dirtyChunkCount == 0)
        {
            _lastMutationTime = null;
            return;
        }

        _lastMutationTime ??= frameTime.TotalTime;

        var timeSinceLastSave = frameTime.TotalTime - _lastSaveTime;
        var timeSinceLastMutation = frameTime.TotalTime - _lastMutationTime.Value;
        var shouldPeriodicSave = timeSinceLastSave >= Options.AutoSaveInterval;
        var shouldIdleSave =
            timeSinceLastMutation >= Options.AutoSaveIdleDelay &&
            timeSinceLastSave >= Options.MinimumAutoSaveSpacing;

        if (!shouldPeriodicSave && !shouldIdleSave)
        {
            return;
        }

        var reason = shouldIdleSave && !shouldPeriodicSave
            ? "Idle"
            : shouldPeriodicSave && !shouldIdleSave
                ? "Interval"
                : "Interval+Idle";
        var savedChunkCount = SaveWorld();
        EngineDiagnostics.Info(
            $"WorldRuntime auto save completed. Reason={reason}, SavedDirtyChunks={savedChunkCount}.");
    }

    private int GetDirtySaveChunkCount()
    {
        var dirtyChunkCount = 0;

        foreach (var _ in DirtyTracker.EnumerateDirtyChunks(World.Chunks.ChunkDirtyFlags.SaveDirty))
        {
            dirtyChunkCount++;
        }

        return dirtyChunkCount;
    }
}
