using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Lighting;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.Runtime.Tracking;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Objects;

namespace TileWorld.Engine.Runtime.Objects;

/// <summary>
/// Manages placed static object instances and their occupancy metadata.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="Runtime.WorldRuntime"/> instead of
/// directly depending on object-instance orchestration details.
/// </remarks>
internal sealed class ObjectManager
{
    private readonly ContentRegistry _contentRegistry;
    private readonly DirtyTracker _dirtyTracker;
    private readonly EntityManager _entityManager;
    private readonly WorldEventBus _eventBus;
    private readonly Dictionary<int, ObjectInstance> _instances = new();
    private readonly Dictionary<ChunkCoord, HashSet<int>> _instancesByChunk = new();
    private readonly Dictionary<ChunkCoord, int[]> _occupancyByChunk = new();
    private readonly WorldQueryService _worldQueryService;
    private LightingSystem _lightingSystem;
    private int _nextInstanceId = 1;

    /// <summary>
    /// Creates an object manager over the supplied world services.
    /// </summary>
    public ObjectManager(
        ContentRegistry contentRegistry,
        WorldQueryService worldQueryService,
        DirtyTracker dirtyTracker,
        WorldEventBus eventBus,
        EntityManager entityManager)
    {
        _contentRegistry = contentRegistry;
        _worldQueryService = worldQueryService;
        _dirtyTracker = dirtyTracker;
        _eventBus = eventBus;
        _entityManager = entityManager;
    }

    public IEnumerable<ObjectInstance> QueryObjectsInChunk(ChunkCoord coord)
    {
        return _instancesByChunk.TryGetValue(coord, out var instanceIds)
            ? instanceIds.Select(id => _instances[id])
            : [];
    }

    public void AttachLightingSystem(LightingSystem lightingSystem)
    {
        _lightingSystem = lightingSystem;
    }

    public bool TryGetObject(int objectInstanceId, out ObjectInstance instance)
    {
        return _instances.TryGetValue(objectInstanceId, out instance!);
    }

    public ObjectInstance GetObject(int objectInstanceId)
    {
        return TryGetObject(objectInstanceId, out var instance)
            ? instance
            : throw new KeyNotFoundException($"No object instance is registered for id {objectInstanceId}.");
    }

    public bool TryGetObjectAt(WorldTileCoord coord, out ObjectInstance instance)
    {
        var chunkCoord = WorldCoordinateConverter.ToChunkCoord(coord);
        var localCoord = WorldCoordinateConverter.ToLocalCoord(coord);
        if (_occupancyByChunk.TryGetValue(chunkCoord, out var occupancy) &&
            occupancy[WorldCoordinateConverter.ToIndex(localCoord.X, localCoord.Y)] is var storedId &&
            storedId != 0 &&
            _instances.TryGetValue(storedId - 1, out instance!))
        {
            return true;
        }

        instance = null!;
        return false;
    }

    public bool IsOccupied(WorldTileCoord coord)
    {
        return TryGetObjectAt(coord, out _);
    }

    public bool TryGetObjectDef(int objectDefId, out ObjectDef objectDef)
    {
        return _contentRegistry.TryGetObjectDef(objectDefId, out objectDef!);
    }

    public Int2 GetFootprintOrigin(WorldTileCoord anchorCoord, ObjectDef objectDef)
    {
        return new Int2(anchorCoord.X - objectDef.AnchorOffset.X, anchorCoord.Y - objectDef.AnchorOffset.Y);
    }

    public IEnumerable<ChunkCoord> EnumerateRelevantChunkCoords(WorldTileCoord coord)
    {
        var center = WorldCoordinateConverter.ToChunkCoord(coord);
        for (var offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
                yield return center.Offset(offsetX, offsetY);
            }
        }
    }

    public bool CanPlaceObject(WorldTileCoord anchorCoord, int objectDefId, ObjectPlacementContext context, Support.SupportSystem supportSystem)
    {
        return ValidatePlacement(anchorCoord, objectDefId, context, supportSystem) == ObjectPlacementErrorCode.None;
    }

    public ObjectPlacementErrorCode ValidatePlacement(
        WorldTileCoord anchorCoord,
        int objectDefId,
        ObjectPlacementContext context,
        Support.SupportSystem supportSystem)
    {
        if (!_contentRegistry.TryGetObjectDef(objectDefId, out var objectDef))
        {
            return ObjectPlacementErrorCode.InvalidObjectDefId;
        }

        if (objectDef.SizeInTiles.X <= 0 || objectDef.SizeInTiles.Y <= 0)
        {
            return ObjectPlacementErrorCode.InvalidAnchor;
        }

        foreach (var footprintCoord in EnumerateFootprint(anchorCoord, objectDef))
        {
            if (!_worldQueryService.IsWithinWorldBounds(footprintCoord))
            {
                return ObjectPlacementErrorCode.OutOfBounds;
            }

            if (IsOccupied(footprintCoord))
            {
                return ObjectPlacementErrorCode.Occupied;
            }

            if (_worldQueryService.GetCell(footprintCoord, new QueryOptions { LoadChunkIfMissing = true }).ForegroundTileId != 0)
            {
                return ObjectPlacementErrorCode.Occupied;
            }
        }

        if (!context.IgnoreValidation && !supportSystem.HasSupport(anchorCoord, objectDef))
        {
            return ObjectPlacementErrorCode.MissingSupport;
        }

        return ObjectPlacementErrorCode.None;
    }

    public ObjectPlacementResult PlaceObject(
        WorldTileCoord anchorCoord,
        int objectDefId,
        ObjectPlacementContext context,
        Support.SupportSystem supportSystem)
    {
        var validationError = ValidatePlacement(anchorCoord, objectDefId, context, supportSystem);
        if (validationError != ObjectPlacementErrorCode.None && !context.IgnoreValidation)
        {
            return ObjectPlacementResult.Failed(validationError, objectDefId, anchorCoord);
        }

        if (!_contentRegistry.TryGetObjectDef(objectDefId, out var objectDef))
        {
            return ObjectPlacementResult.Failed(ObjectPlacementErrorCode.InvalidObjectDefId, objectDefId, anchorCoord);
        }

        var instance = new ObjectInstance
        {
            InstanceId = _nextInstanceId++,
            ObjectDefId = objectDefId,
            AnchorCoord = anchorCoord,
            Direction = context.Direction
        };

        RegisterInstance(instance, objectDef);
        var dirtyFlags = MarkOccupiedChunksDirty(instance, objectDef);

        if (!context.SuppressEvents)
        {
            _eventBus.Publish(new ObjectPlacedEvent(
                instance.InstanceId,
                instance.ObjectDefId,
                instance.AnchorCoord,
                instance.Direction,
                context.ActorEntityId));
        }

        return ObjectPlacementResult.Succeeded(instance.InstanceId, objectDefId, anchorCoord, dirtyFlags);
    }

    public bool RemoveObject(int objectInstanceId, bool destroyed, bool spawnDrop, bool publishEvents)
    {
        if (!_instances.TryGetValue(objectInstanceId, out var instance) ||
            !_contentRegistry.TryGetObjectDef(instance.ObjectDefId, out var objectDef))
        {
            return false;
        }

        UnregisterInstance(instance, objectDef);
        MarkOccupiedChunksDirty(instance, objectDef);

        if (destroyed && spawnDrop && objectDef.BreakDropItemId != 0)
        {
            var dropPosition = GetObjectCenter(instance, objectDef);
            _entityManager.SpawnDrop(objectDef.BreakDropItemId, dropPosition);
        }

        if (publishEvents)
        {
            _eventBus.Publish(new ObjectRemovedEvent(
                instance.InstanceId,
                instance.ObjectDefId,
                instance.AnchorCoord,
                destroyed));
        }

        return true;
    }

    public void RegisterLoadedObject(ObjectInstance instance)
    {
        if (!_contentRegistry.TryGetObjectDef(instance.ObjectDefId, out var objectDef))
        {
            return;
        }

        _nextInstanceId = Math.Max(_nextInstanceId, instance.InstanceId + 1);
        RegisterInstance(instance, objectDef);
    }

    public void RemoveObjectsAnchoredInChunk(ChunkCoord chunkCoord)
    {
        var instanceIds = _instances.Values
            .Where(instance =>
                _contentRegistry.TryGetObjectDef(instance.ObjectDefId, out var objectDef) &&
                WorldCoordinateConverter.ToChunkCoord(new WorldTileCoord(instance.AnchorCoord.X - objectDef.AnchorOffset.X, instance.AnchorCoord.Y - objectDef.AnchorOffset.Y)) == chunkCoord)
            .Select(instance => instance.InstanceId)
            .ToArray();

        foreach (var instanceId in instanceIds)
        {
            RemoveObject(instanceId, destroyed: false, spawnDrop: false, publishEvents: false);
        }
    }

    public IReadOnlyList<ObjectInstance> GetPersistedObjectsForChunk(ChunkCoord chunkCoord)
    {
        var instances = new List<ObjectInstance>();

        foreach (var instance in _instances.Values)
        {
            if (!_contentRegistry.TryGetObjectDef(instance.ObjectDefId, out var objectDef))
            {
                continue;
            }

            var origin = GetFootprintOrigin(instance.AnchorCoord, objectDef);
            var anchorChunkCoord = WorldCoordinateConverter.ToChunkCoord(new WorldTileCoord(origin.X, origin.Y));
            if (anchorChunkCoord == chunkCoord)
            {
                instances.Add(instance);
            }
        }

        return instances;
    }

    private void RegisterInstance(ObjectInstance instance, ObjectDef objectDef)
    {
        _instances[instance.InstanceId] = instance;

        foreach (var coord in EnumerateFootprint(instance.AnchorCoord, objectDef))
        {
            var chunkCoord = WorldCoordinateConverter.ToChunkCoord(coord);
            var localCoord = WorldCoordinateConverter.ToLocalCoord(coord);
            var occupancy = GetOrCreateOccupancy(chunkCoord);
            occupancy[WorldCoordinateConverter.ToIndex(localCoord.X, localCoord.Y)] = instance.InstanceId + 1;

            if (!_instancesByChunk.TryGetValue(chunkCoord, out var chunkInstances))
            {
                chunkInstances = new HashSet<int>();
                _instancesByChunk.Add(chunkCoord, chunkInstances);
            }

            chunkInstances.Add(instance.InstanceId);
        }
    }

    private void UnregisterInstance(ObjectInstance instance, ObjectDef objectDef)
    {
        _instances.Remove(instance.InstanceId);

        foreach (var coord in EnumerateFootprint(instance.AnchorCoord, objectDef))
        {
            var chunkCoord = WorldCoordinateConverter.ToChunkCoord(coord);
            var localCoord = WorldCoordinateConverter.ToLocalCoord(coord);
            if (_occupancyByChunk.TryGetValue(chunkCoord, out var occupancy))
            {
                occupancy[WorldCoordinateConverter.ToIndex(localCoord.X, localCoord.Y)] = 0;
            }

            if (_instancesByChunk.TryGetValue(chunkCoord, out var chunkInstances))
            {
                chunkInstances.Remove(instance.InstanceId);
                if (chunkInstances.Count == 0)
                {
                    _instancesByChunk.Remove(chunkCoord);
                }
            }
        }
    }

    private ChunkDirtyFlags MarkOccupiedChunksDirty(ObjectInstance instance, ObjectDef objectDef)
    {
        var affectedChunks = new HashSet<ChunkCoord>();
        foreach (var coord in EnumerateFootprint(instance.AnchorCoord, objectDef))
        {
            affectedChunks.Add(WorldCoordinateConverter.ToChunkCoord(coord));
        }

        var dirtyFlags = ChunkDirtyFlags.RenderDirty | ChunkDirtyFlags.SaveDirty;
        if (objectDef.EmissiveLight > 0)
        {
            dirtyFlags |= ChunkDirtyFlags.LightDirty;
        }

        foreach (var chunkCoord in affectedChunks)
        {
            _dirtyTracker.MarkDirty(chunkCoord, dirtyFlags);
            if (objectDef.EmissiveLight > 0)
            {
                _dirtyTracker.MarkSurroundingLoadedDirty(chunkCoord, ChunkDirtyFlags.LightDirty, includeCenter: false);
            }
        }

        if (objectDef.EmissiveLight > 0)
        {
            _lightingSystem?.MarkDirty(instance.AnchorCoord);
        }

        return dirtyFlags;
    }

    private IEnumerable<WorldTileCoord> EnumerateFootprint(WorldTileCoord anchorCoord, ObjectDef objectDef)
    {
        var origin = GetFootprintOrigin(anchorCoord, objectDef);
        for (var localY = 0; localY < objectDef.SizeInTiles.Y; localY++)
        {
            for (var localX = 0; localX < objectDef.SizeInTiles.X; localX++)
            {
                yield return new WorldTileCoord(origin.X + localX, origin.Y + localY);
            }
        }
    }

    private int[] GetOrCreateOccupancy(ChunkCoord chunkCoord)
    {
        if (_occupancyByChunk.TryGetValue(chunkCoord, out var occupancy))
        {
            return occupancy;
        }

        occupancy = new int[ChunkDimensions.CellCount];
        _occupancyByChunk.Add(chunkCoord, occupancy);
        return occupancy;
    }

    private Float2 GetObjectCenter(ObjectInstance instance, ObjectDef objectDef)
    {
        var origin = GetFootprintOrigin(instance.AnchorCoord, objectDef);
        return new Float2(origin.X + (objectDef.SizeInTiles.X / 2f), origin.Y + (objectDef.SizeInTiles.Y / 2f));
    }
}
