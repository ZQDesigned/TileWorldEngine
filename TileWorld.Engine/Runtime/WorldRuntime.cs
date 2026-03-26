using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Runtime.AutoTile;
using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Edits;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Objects;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.Runtime.Support;
using TileWorld.Engine.Runtime.Tracking;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Objects;

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
        EventBus = new WorldEventBus();
        EventBus.Subscribe<TileChangedEvent>(_ => _pendingMutationObserved = true);
        EventBus.Subscribe<ObjectPlacedEvent>(_ => _pendingMutationObserved = true);
        EventBus.Subscribe<ObjectRemovedEvent>(_ => _pendingMutationObserved = true);

        Storage = CreateWorldStorage(Options);
        ChunkManager = CreateChunkManager(worldData, Storage, Options, EventBus);
        QueryService = new WorldQueryService(worldData, contentRegistry, chunkManager: ChunkManager);
        DirtyTracker = new DirtyTracker(worldData);
        EntityManager = new EntityManager(new TileCollisionService(QueryService), contentRegistry, EventBus);
        AutoTileSystem = new AutoTileSystem(worldData, QueryService);
        ObjectManager = new ObjectManager(contentRegistry, QueryService, DirtyTracker, EventBus, EntityManager);
        SupportSystem = new SupportSystem(ObjectManager, QueryService);
        QueryService.AttachObjectManager(ObjectManager);
        ChunkManager?.AttachObjectManager(ObjectManager);
        TileEditService = new TileEditService(
            worldData,
            contentRegistry,
            QueryService,
            DirtyTracker,
            EventBus,
            AutoTileSystem,
            ObjectManager,
            SupportSystem,
            EntityManager,
            ChunkManager);
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

    internal SupportSystem SupportSystem { get; }

    internal ObjectManager ObjectManager { get; }

    internal TileEditService TileEditService { get; }

    internal EntityManager EntityManager { get; }

    /// <summary>
    /// Transitions the runtime into the initialized state.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        RestorePersistedEntitiesIfNeeded();
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
        EntityManager.Update(frameTime);
        if (EntityManager.ConsumePersistenceMutationObserved())
        {
            _pendingMutationObserved = true;
        }

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
    /// Returns whether the supplied coordinate contains a background wall.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns><see langword="true"/> when a background wall is present.</returns>
    public bool HasBackgroundWall(WorldTileCoord coord)
    {
        return QueryService.HasBackgroundWall(coord);
    }

    /// <summary>
    /// Writes a background wall directly to the world.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <param name="wallId">The non-empty wall identifier to write.</param>
    /// <returns><see langword="true"/> when the write succeeds.</returns>
    public bool SetBackgroundWall(WorldTileCoord coord, ushort wallId)
    {
        return TileEditService.SetBackgroundWall(coord, wallId);
    }

    /// <summary>
    /// Removes the background wall at the supplied coordinate.
    /// </summary>
    /// <param name="coord">The target world-tile coordinate.</param>
    /// <returns><see langword="true"/> when a background wall existed and was removed.</returns>
    public bool RemoveBackgroundWall(WorldTileCoord coord)
    {
        return TileEditService.RemoveBackgroundWall(coord);
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
    /// Loads or creates a chunk and returns detailed source information.
    /// </summary>
    /// <param name="coord">The chunk coordinate to load or create.</param>
    /// <returns>The detailed chunk load result.</returns>
    public ChunkLoadResult LoadChunk(ChunkCoord coord)
    {
        return ChunkManager is not null
            ? ChunkManager.GetOrLoadChunkDetailed(coord)
            : new ChunkLoadResult(WorldData.GetOrCreateChunk(coord), true, false, false);
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
    /// Ensures the active chunk set around the supplied world-tile coordinate.
    /// </summary>
    /// <param name="center">The world-tile coordinate that should remain centered in the active set.</param>
    public void EnsureActiveAround(WorldTileCoord center)
    {
        ChunkManager?.EnsureActiveAround(center);
    }

    /// <summary>
    /// Enumerates the currently active chunk coordinates.
    /// </summary>
    /// <returns>The active chunk coordinates.</returns>
    public IEnumerable<ChunkCoord> GetActiveChunks()
    {
        return ChunkManager?.GetActiveChunks() ?? [];
    }

    /// <summary>
    /// Evaluates whether an object placement would currently succeed.
    /// </summary>
    /// <param name="anchorCoord">The logical object anchor coordinate.</param>
    /// <param name="objectDefId">The object definition identifier to place.</param>
    /// <param name="context">Placement metadata and behavior flags.</param>
    /// <returns><see langword="true"/> when placement would currently succeed.</returns>
    public bool CanPlaceObject(WorldTileCoord anchorCoord, int objectDefId, ObjectPlacementContext context)
    {
        return ObjectManager.CanPlaceObject(anchorCoord, objectDefId, context, SupportSystem);
    }

    /// <summary>
    /// Places an object instance into the world.
    /// </summary>
    /// <param name="anchorCoord">The logical object anchor coordinate.</param>
    /// <param name="objectDefId">The object definition identifier to place.</param>
    /// <param name="context">Placement metadata and behavior flags.</param>
    /// <returns>The outcome of the placement attempt.</returns>
    public ObjectPlacementResult PlaceObject(WorldTileCoord anchorCoord, int objectDefId, ObjectPlacementContext context)
    {
        if (ChunkManager is not null &&
            ContentRegistry.TryGetObjectDef(objectDefId, out var objectDef))
        {
            var origin = ObjectManager.GetFootprintOrigin(anchorCoord, objectDef);
            var minChunk = WorldCoordinateConverter.ToChunkCoord(new WorldTileCoord(origin.X, origin.Y));
            var maxChunk = WorldCoordinateConverter.ToChunkCoord(new WorldTileCoord(
                origin.X + objectDef.SizeInTiles.X - 1,
                origin.Y + objectDef.SizeInTiles.Y - 1));

            for (var chunkY = minChunk.Y; chunkY <= maxChunk.Y; chunkY++)
            {
                for (var chunkX = minChunk.X; chunkX <= maxChunk.X; chunkX++)
                {
                    ChunkManager.GetOrLoadChunk(new ChunkCoord(chunkX, chunkY));
                }
            }
        }

        return ObjectManager.PlaceObject(anchorCoord, objectDefId, context, SupportSystem);
    }

    /// <summary>
    /// Removes an object instance from the world.
    /// </summary>
    /// <param name="objectInstanceId">The object instance identifier to remove.</param>
    /// <param name="destroyed">Whether the removal should be treated as destruction.</param>
    /// <returns><see langword="true"/> when the object instance existed and was removed.</returns>
    public bool RemoveObject(int objectInstanceId, bool destroyed = true)
    {
        return ObjectManager.RemoveObject(objectInstanceId, destroyed, spawnDrop: destroyed, publishEvents: true);
    }

    /// <summary>
    /// Resolves an object instance by identifier.
    /// </summary>
    /// <param name="objectInstanceId">The object instance identifier to resolve.</param>
    /// <returns>The resolved object instance.</returns>
    public ObjectInstance GetObject(int objectInstanceId)
    {
        return ObjectManager.GetObject(objectInstanceId);
    }

    /// <summary>
    /// Attempts to resolve an object instance by identifier.
    /// </summary>
    /// <param name="objectInstanceId">The object instance identifier to resolve.</param>
    /// <param name="instance">The resolved object instance when present.</param>
    /// <returns><see langword="true"/> when the instance exists.</returns>
    public bool TryGetObject(int objectInstanceId, out ObjectInstance instance)
    {
        return ObjectManager.TryGetObject(objectInstanceId, out instance);
    }

    /// <summary>
    /// Attempts to resolve an object instance occupying a world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <param name="instance">The resolved object instance when present.</param>
    /// <returns><see langword="true"/> when an object occupies the coordinate.</returns>
    public bool TryGetObjectAt(WorldTileCoord coord, out ObjectInstance instance)
    {
        return QueryService.TryGetObjectAt(coord, out instance);
    }

    /// <summary>
    /// Spawns a controllable player prototype at the supplied world position in tile units.
    /// </summary>
    /// <param name="position">The player spawn position in world tile units.</param>
    /// <returns>The created player entity identifier.</returns>
    public int SpawnPlayer(Float2 position)
    {
        return EntityManager.SpawnPlayer(position);
    }

    /// <summary>
    /// Applies movement input to a player entity for the next update.
    /// </summary>
    /// <param name="entityId">The target player entity identifier.</param>
    /// <param name="moveAxis">The horizontal movement axis in the range [-1, 1].</param>
    /// <param name="jumpRequested">Whether a jump should be attempted.</param>
    public void SetPlayerInput(int entityId, float moveAxis, bool jumpRequested)
    {
        EntityManager.SetPlayerInput(entityId, moveAxis, jumpRequested);
    }

    /// <summary>
    /// Attempts to resolve an entity by identifier.
    /// </summary>
    /// <param name="entityId">The entity identifier to resolve.</param>
    /// <param name="entity">The resolved entity when present.</param>
    /// <returns><see langword="true"/> when the entity exists.</returns>
    public bool TryGetEntity(int entityId, out Entity entity)
    {
        return EntityManager.TryGetEntity(entityId, out entity);
    }

    /// <summary>
    /// Enumerates all active prototype entities.
    /// </summary>
    /// <returns>The active entities.</returns>
    public IEnumerable<Entity> EnumerateEntities()
    {
        return EntityManager.EnumerateEntities();
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

        NormalizeMetadataBeforeSave();
        Storage.SaveMetadata(Options.WorldPath, WorldData.Metadata);
        var savedChunkCount = ChunkManager.SaveDirtyChunks();
        SavePersistentEntityState();
        _lastSaveTime = _lastObservedUpdateTime;

        if (GetDirtySaveChunkCount() == 0 && !EntityManager.HasPendingPersistenceChanges)
        {
            _lastMutationTime = null;
        }

        return savedChunkCount;
    }

    private void NormalizeMetadataBeforeSave()
    {
        var metadata = WorldData.Metadata;
        if (metadata.ChunkFormatVersion == 2 &&
            metadata.ChunkWidth == World.Chunks.ChunkDimensions.Width &&
            metadata.ChunkHeight == World.Chunks.ChunkDimensions.Height)
        {
            return;
        }

        WorldData.UpdateMetadata(new WorldMetadata
        {
            WorldId = metadata.WorldId,
            Name = metadata.Name,
            Seed = metadata.Seed,
            WorldFormatVersion = metadata.WorldFormatVersion,
            ChunkFormatVersion = 2,
            WorldTime = metadata.WorldTime,
            BoundsMode = metadata.BoundsMode,
            SpawnTile = metadata.SpawnTile,
            ChunkWidth = World.Chunks.ChunkDimensions.Width,
            ChunkHeight = World.Chunks.ChunkDimensions.Height
        });
    }

    private static WorldStorage CreateWorldStorage(WorldRuntimeOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.WorldPath)
            ? options.WorldStorage ?? new WorldStorage()
            : null;
    }

    private static ChunkManager CreateChunkManager(
        WorldData worldData,
        WorldStorage storage,
        WorldRuntimeOptions options,
        WorldEventBus eventBus)
    {
        return storage is not null
            ? new ChunkManager(worldData, storage, options.WorldPath, options.ActiveRadiusInChunks, eventBus)
            : null;
    }

    private void UpdateAutoSave(FrameTime frameTime)
    {
        if (!IsPersistenceEnabled || !Options.EnableAutoSave)
        {
            return;
        }

        var dirtyChunkCount = GetDirtySaveChunkCount();
        var hasPendingEntityPersistence = EntityManager.HasPendingPersistenceChanges;
        if (dirtyChunkCount == 0 && !hasPendingEntityPersistence)
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
            $"WorldRuntime auto save completed. Reason={reason}, SavedDirtyChunks={savedChunkCount}, SavedEntities={GetPersistedEntityCount()}.");
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

    private void RestorePersistedEntitiesIfNeeded()
    {
        if (!IsPersistenceEnabled || EntityManager.EnumerateEntities().Any())
        {
            return;
        }

        var persistedPlayers = Storage.LoadPlayers(Options.WorldPath);
        var persistedRuntimeEntities = Storage.LoadRuntimeEntities(Options.WorldPath);
        if (persistedPlayers.Count == 0 && persistedRuntimeEntities.Count == 0)
        {
            return;
        }

        EntityManager.RestorePersistentState(persistedPlayers, persistedRuntimeEntities);
        EngineDiagnostics.Info(
            $"WorldRuntime restored persisted entities. Players={persistedPlayers.Count}, RuntimeEntities={persistedRuntimeEntities.Count}.");
    }

    private void SavePersistentEntityState()
    {
        var players = EntityManager.GetPersistedPlayers();
        var runtimeEntities = EntityManager.GetPersistedRuntimeEntities();
        Storage.SavePlayers(Options.WorldPath, players);
        Storage.SaveRuntimeEntities(Options.WorldPath, runtimeEntities);
        EntityManager.MarkPersistenceChangesSaved();
        EngineDiagnostics.Info(
            $"WorldRuntime persisted entity state. Players={players.Count}, RuntimeEntities={runtimeEntities.Count}.");
    }

    private int GetPersistedEntityCount()
    {
        return EntityManager.GetPersistedPlayers().Count + EntityManager.GetPersistedRuntimeEntities().Count;
    }
}
