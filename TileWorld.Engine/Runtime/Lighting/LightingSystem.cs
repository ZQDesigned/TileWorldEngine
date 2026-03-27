using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.Objects;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.Runtime.Tracking;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Generation;

namespace TileWorld.Engine.Runtime.Lighting;

/// <summary>
/// Rebuilds derived chunk-local light buffers for loaded world regions.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="Runtime.WorldRuntime"/> instead of
/// depending on transient lighting-cache orchestration details.
/// </remarks>
internal sealed class LightingSystem
{
    private const byte MaxLightLevel = 15;
    private readonly ContentRegistry _contentRegistry;
    private readonly DirtyTracker _dirtyTracker;
    private readonly EntityManager _entityManager;
    private readonly ObjectManager _objectManager;
    private readonly WorldQueryService _queryService;
    private readonly IWorldGenerator _worldGenerator;
    private readonly Dictionary<int, DynamicLightState> _dynamicLightStates = new();
    private readonly Dictionary<ChunkCoord, ChunkLightBuffer> _lightBuffers = new();
    private readonly WorldData _worldData;

    /// <summary>
    /// Creates a lighting system over the supplied world services.
    /// </summary>
    /// <param name="worldData">The world data whose loaded chunks should receive derived lighting buffers.</param>
    /// <param name="contentRegistry">The content registry used to resolve emissive definitions.</param>
    /// <param name="queryService">The query service used to inspect tile and wall state.</param>
    /// <param name="dirtyTracker">The dirty tracker used to mark chunk lighting work.</param>
    /// <param name="objectManager">The object manager used to resolve emissive object instances.</param>
    /// <param name="entityManager">The entity manager used to resolve dynamic light sources such as the player.</param>
    /// <param name="worldGenerator">The generator used to resolve deterministic surface heights for skylight seeding.</param>
    public LightingSystem(
        WorldData worldData,
        ContentRegistry contentRegistry,
        WorldQueryService queryService,
        DirtyTracker dirtyTracker,
        ObjectManager objectManager,
        EntityManager entityManager,
        IWorldGenerator worldGenerator)
    {
        _worldData = worldData ?? throw new ArgumentNullException(nameof(worldData));
        _contentRegistry = contentRegistry ?? throw new ArgumentNullException(nameof(contentRegistry));
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _dirtyTracker = dirtyTracker ?? throw new ArgumentNullException(nameof(dirtyTracker));
        _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        _worldGenerator = worldGenerator ?? throw new ArgumentNullException(nameof(worldGenerator));
    }

    /// <summary>
    /// Marks the chunk containing a world-tile coordinate, plus loaded surrounding chunks, as lighting-dirty.
    /// </summary>
    /// <param name="coord">The world-tile coordinate that changed.</param>
    public void MarkDirty(WorldTileCoord coord)
    {
        var chunkCoord = WorldCoordinateConverter.ToChunkCoord(coord);
        _dirtyTracker.MarkLoadedDirty(chunkCoord, ChunkDirtyFlags.LightDirty);
        _dirtyTracker.MarkSurroundingLoadedDirty(chunkCoord, ChunkDirtyFlags.LightDirty, includeCenter: false);
    }

    /// <summary>
    /// Marks a loaded chunk as lighting-dirty.
    /// </summary>
    /// <param name="coord">The chunk coordinate that should be rebuilt.</param>
    public void MarkChunkDirty(ChunkCoord coord)
    {
        _dirtyTracker.MarkLoadedDirty(coord, ChunkDirtyFlags.LightDirty);
    }

    /// <summary>
    /// Rebuilds lighting for a bounded number of dirty active chunks.
    /// </summary>
    /// <param name="activeChunks">The chunk coordinates that are currently relevant to runtime updates and rendering.</param>
    /// <param name="maxChunksPerFrame">The maximum number of dirty chunks rebuilt in one update tick.</param>
    public void RebuildDirtyLighting(IEnumerable<ChunkCoord> activeChunks, int maxChunksPerFrame)
    {
        ArgumentNullException.ThrowIfNull(activeChunks);

        var activeChunkSet = activeChunks.ToHashSet();
        if (activeChunkSet.Count == 0)
        {
            return;
        }

        var dirtyActiveChunks = _dirtyTracker
            .EnumerateDirtyChunks(ChunkDirtyFlags.LightDirty)
            .Where(activeChunkSet.Contains)
            .OrderBy(coord => coord.Y)
            .ThenBy(coord => coord.X)
            .Take(Math.Max(1, maxChunksPerFrame))
            .ToArray();

        foreach (var chunkCoord in dirtyActiveChunks)
        {
            RebuildChunk(chunkCoord);
            _dirtyTracker.ClearDirty(chunkCoord, ChunkDirtyFlags.LightDirty);
        }

        if (dirtyActiveChunks.Length > 0)
        {
            EngineDiagnostics.Info($"LightingSystem rebuilt chunk lighting for: {string.Join(", ", dirtyActiveChunks)}.");
        }
    }

    /// <summary>
    /// Attempts to resolve a light level at a world-tile coordinate.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <param name="lightLevel">The resolved light level when available.</param>
    /// <returns><see langword="true"/> when the chunk was available and a light value was resolved.</returns>
    public bool TryGetLightLevel(WorldTileCoord coord, out byte lightLevel)
    {
        if (!_queryService.IsWithinWorldBounds(coord))
        {
            lightLevel = 0;
            return false;
        }

        var chunkCoord = WorldCoordinateConverter.ToChunkCoord(coord);
        if (!_worldData.TryGetChunk(chunkCoord, out _))
        {
            lightLevel = 0;
            return false;
        }

        EnsureBufferForChunk(chunkCoord);
        if (!_lightBuffers.TryGetValue(chunkCoord, out var lightBuffer))
        {
            lightLevel = 0;
            return false;
        }

        var localCoord = WorldCoordinateConverter.ToLocalCoord(coord);
        lightLevel = lightBuffer.GetLightLevel(localCoord.X, localCoord.Y);
        return true;
    }

    /// <summary>
    /// Resolves a light level at a world-tile coordinate, returning darkness when unavailable.
    /// </summary>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns>The resolved light level, or <c>0</c> when unavailable.</returns>
    public byte GetLightLevel(WorldTileCoord coord)
    {
        return TryGetLightLevel(coord, out var lightLevel)
            ? lightLevel
            : (byte)0;
    }

    /// <summary>
    /// Removes cached lighting for an unloaded chunk.
    /// </summary>
    /// <param name="coord">The unloaded chunk coordinate.</param>
    public void RemoveChunkBuffer(ChunkCoord coord)
    {
        _lightBuffers.Remove(coord);
    }

    /// <summary>
    /// Marks every currently loaded chunk as lighting-dirty.
    /// </summary>
    public void MarkAllLoadedChunksDirty()
    {
        foreach (var chunk in _worldData.EnumerateLoadedChunks())
        {
            chunk.DirtyFlags |= ChunkDirtyFlags.LightDirty;
        }
    }

    /// <summary>
    /// Synchronizes dynamic runtime light sources and invalidates nearby lighting when they move or change intensity.
    /// </summary>
    public void SyncDynamicLightSources()
    {
        var seenEntityIds = new HashSet<int>();

        foreach (var entity in _entityManager.EnumerateEntities())
        {
            var lightLevel = _entityManager.GetResolvedEmissiveLight(entity);
            if (lightLevel == 0)
            {
                continue;
            }

            seenEntityIds.Add(entity.EntityId);
            var centerCoord = GetEntityCenterTile(entity);
            if (!_dynamicLightStates.TryGetValue(entity.EntityId, out var previousState))
            {
                _dynamicLightStates[entity.EntityId] = new DynamicLightState(centerCoord, lightLevel);
                MarkDirty(centerCoord);
                continue;
            }

            if (previousState.Coord != centerCoord || previousState.LightLevel != lightLevel)
            {
                MarkDirty(previousState.Coord);
                MarkDirty(centerCoord);
                _dynamicLightStates[entity.EntityId] = new DynamicLightState(centerCoord, lightLevel);
            }
        }

        foreach (var entityId in _dynamicLightStates.Keys.Except(seenEntityIds).ToArray())
        {
            MarkDirty(_dynamicLightStates[entityId].Coord);
            _dynamicLightStates.Remove(entityId);
        }
    }

    private void EnsureBufferForChunk(ChunkCoord chunkCoord)
    {
        if (!_worldData.TryGetChunk(chunkCoord, out _))
        {
            return;
        }

        if (_lightBuffers.ContainsKey(chunkCoord) &&
            !_dirtyTracker.HasDirty(chunkCoord, ChunkDirtyFlags.LightDirty))
        {
            return;
        }

        RebuildChunk(chunkCoord);
        _dirtyTracker.ClearDirty(chunkCoord, ChunkDirtyFlags.LightDirty);
    }

    private void RebuildChunk(ChunkCoord chunkCoord)
    {
        if (!_worldData.TryGetChunk(chunkCoord, out _))
        {
            _lightBuffers.Remove(chunkCoord);
            return;
        }

        var minChunkX = chunkCoord.X - 1;
        var maxChunkX = chunkCoord.X + 1;
        var minChunkY = chunkCoord.Y - 1;
        var maxChunkY = chunkCoord.Y + 1;
        var windowWidth = ChunkDimensions.Width * 3;
        var windowHeight = ChunkDimensions.Height * 3;
        var windowOriginX = minChunkX * ChunkDimensions.Width;
        var windowOriginY = minChunkY * ChunkDimensions.Height;
        var lightLevels = new byte[windowWidth * windowHeight];
        var blockingMask = new bool[windowWidth * windowHeight];
        var emissiveLevels = new byte[windowWidth * windowHeight];
        var propagationQueue = new Queue<(int X, int Y)>();
        var generationContext = new WorldGenerationContext
        {
            Metadata = _worldData.Metadata,
            ContentRegistry = _contentRegistry
        };

        for (var localY = 0; localY < windowHeight; localY++)
        {
            var worldY = windowOriginY + localY;
            for (var localX = 0; localX < windowWidth; localX++)
            {
                var worldX = windowOriginX + localX;
                var worldCoord = new WorldTileCoord(worldX, worldY);
                blockingMask[ToWindowIndex(localX, localY, windowWidth)] = _queryService.BlocksLight(worldCoord);
                emissiveLevels[ToWindowIndex(localX, localY, windowWidth)] = GetTileEmissiveLight(worldCoord);
            }
        }

        SeedSkyLight(
            generationContext,
            windowOriginX,
            windowOriginY,
            windowWidth,
            windowHeight,
            blockingMask,
            lightLevels,
            propagationQueue);
        SeedDynamicEntityLight(windowOriginX, windowOriginY, windowWidth, windowHeight, lightLevels, emissiveLevels, propagationQueue);
        SeedObjectLight(windowOriginX, windowOriginY, windowWidth, windowHeight, lightLevels, emissiveLevels, propagationQueue);
        SeedTileEmissiveLight(windowWidth, windowHeight, lightLevels, emissiveLevels, propagationQueue);
        PropagateLight(windowWidth, windowHeight, blockingMask, lightLevels, propagationQueue);

        var lightBuffer = new ChunkLightBuffer(chunkCoord);
        for (var targetLocalY = 0; targetLocalY < ChunkDimensions.Height; targetLocalY++)
        {
            for (var targetLocalX = 0; targetLocalX < ChunkDimensions.Width; targetLocalX++)
            {
                var windowLocalX = targetLocalX + ChunkDimensions.Width;
                var windowLocalY = targetLocalY + ChunkDimensions.Height;
                var windowIndex = ToWindowIndex(windowLocalX, windowLocalY, windowWidth);
                lightBuffer.SetLightLevel(targetLocalX, targetLocalY, lightLevels[windowIndex]);
            }
        }

        _lightBuffers[chunkCoord] = lightBuffer;
    }

    private void SeedSkyLight(
        WorldGenerationContext generationContext,
        int windowOriginX,
        int windowOriginY,
        int windowWidth,
        int windowHeight,
        IReadOnlyList<bool> blockingMask,
        byte[] lightLevels,
        Queue<(int X, int Y)> propagationQueue)
    {
        for (var localX = 0; localX < windowWidth; localX++)
        {
            var worldX = windowOriginX + localX;
            var surfaceY = ResolveSkySurfaceY(generationContext, worldX);

            for (var localY = 0; localY < windowHeight; localY++)
            {
                var worldY = windowOriginY + localY;
                var directSkyLight = ResolveDirectSkyLightLevel(
                    worldX,
                    surfaceY,
                    worldY,
                    localX,
                    windowOriginY,
                    windowWidth,
                    blockingMask);
                if (directSkyLight == 0)
                {
                    continue;
                }

                var windowIndex = ToWindowIndex(localX, localY, windowWidth);
                lightLevels[windowIndex] = Max(lightLevels[windowIndex], directSkyLight);
            }
        }
    }

    private void SeedDynamicEntityLight(
        int windowOriginX,
        int windowOriginY,
        int windowWidth,
        int windowHeight,
        byte[] lightLevels,
        byte[] emissiveLevels,
        Queue<(int X, int Y)> propagationQueue)
    {
        foreach (var entity in _entityManager.EnumerateEntities())
        {
            var lightLevel = _entityManager.GetResolvedEmissiveLight(entity);
            if (lightLevel == 0)
            {
                continue;
            }

            var centerCoord = GetEntityCenterTile(entity);
            var localX = centerCoord.X - windowOriginX;
            var localY = centerCoord.Y - windowOriginY;
            if (localX < 0 || localX >= windowWidth || localY < 0 || localY >= windowHeight)
            {
                continue;
            }

            var windowIndex = ToWindowIndex(localX, localY, windowWidth);
            if (lightLevels[windowIndex] < lightLevel)
            {
                lightLevels[windowIndex] = lightLevel;
            }

            if (emissiveLevels[windowIndex] < lightLevel)
            {
                emissiveLevels[windowIndex] = lightLevel;
            }

            propagationQueue.Enqueue((localX, localY));
        }
    }

    private void SeedObjectLight(
        int windowOriginX,
        int windowOriginY,
        int windowWidth,
        int windowHeight,
        byte[] lightLevels,
        byte[] emissiveLevels,
        Queue<(int X, int Y)> propagationQueue)
    {
        var seenObjectIds = new HashSet<int>();
        for (var chunkY = 0; chunkY < 3; chunkY++)
        {
            for (var chunkX = 0; chunkX < 3; chunkX++)
            {
                var coord = new ChunkCoord(
                    (windowOriginX / ChunkDimensions.Width) + chunkX,
                    (windowOriginY / ChunkDimensions.Height) + chunkY);

                foreach (var instance in _objectManager.QueryObjectsInChunk(coord))
                {
                    if (!seenObjectIds.Add(instance.InstanceId) ||
                        !_objectManager.TryGetObjectDef(instance.ObjectDefId, out var objectDef) ||
                        objectDef.EmissiveLight == 0)
                    {
                        continue;
                    }

                    var origin = _objectManager.GetFootprintOrigin(instance.AnchorCoord, objectDef);
                    var sourceX = origin.X + (objectDef.SizeInTiles.X / 2);
                    var sourceY = origin.Y + (objectDef.SizeInTiles.Y / 2);
                    var localX = sourceX - windowOriginX;
                    var localY = sourceY - windowOriginY;
                    if (localX < 0 || localX >= windowWidth || localY < 0 || localY >= windowHeight)
                    {
                        continue;
                    }

                    var windowIndex = ToWindowIndex(localX, localY, windowWidth);
                    if (lightLevels[windowIndex] < objectDef.EmissiveLight)
                    {
                        lightLevels[windowIndex] = objectDef.EmissiveLight;
                    }

                    if (emissiveLevels[windowIndex] < objectDef.EmissiveLight)
                    {
                        emissiveLevels[windowIndex] = objectDef.EmissiveLight;
                    }

                    propagationQueue.Enqueue((localX, localY));
                }
            }
        }
    }

    private static void SeedTileEmissiveLight(
        int windowWidth,
        int windowHeight,
        byte[] lightLevels,
        byte[] emissiveLevels,
        Queue<(int X, int Y)> propagationQueue)
    {
        for (var localY = 0; localY < windowHeight; localY++)
        {
            for (var localX = 0; localX < windowWidth; localX++)
            {
                var windowIndex = ToWindowIndex(localX, localY, windowWidth);
                if (emissiveLevels[windowIndex] == 0 || lightLevels[windowIndex] >= emissiveLevels[windowIndex])
                {
                    continue;
                }

                lightLevels[windowIndex] = emissiveLevels[windowIndex];
                propagationQueue.Enqueue((localX, localY));
            }
        }
    }

    private static void PropagateLight(
        int windowWidth,
        int windowHeight,
        IReadOnlyList<bool> blockingMask,
        byte[] lightLevels,
        Queue<(int X, int Y)> propagationQueue)
    {
        while (propagationQueue.Count > 0)
        {
            var (localX, localY) = propagationQueue.Dequeue();
            var currentLevel = lightLevels[ToWindowIndex(localX, localY, windowWidth)];
            if (currentLevel <= 1)
            {
                continue;
            }

            TryPropagate(localX, localY - 1, currentLevel, windowWidth, windowHeight, blockingMask, lightLevels, propagationQueue);
            TryPropagate(localX + 1, localY, currentLevel, windowWidth, windowHeight, blockingMask, lightLevels, propagationQueue);
            TryPropagate(localX, localY + 1, currentLevel, windowWidth, windowHeight, blockingMask, lightLevels, propagationQueue);
            TryPropagate(localX - 1, localY, currentLevel, windowWidth, windowHeight, blockingMask, lightLevels, propagationQueue);
        }
    }

    private static void TryPropagate(
        int localX,
        int localY,
        byte sourceLevel,
        int windowWidth,
        int windowHeight,
        IReadOnlyList<bool> blockingMask,
        byte[] lightLevels,
        Queue<(int X, int Y)> propagationQueue)
    {
        if (localX < 0 || localX >= windowWidth || localY < 0 || localY >= windowHeight)
        {
            return;
        }

        var windowIndex = ToWindowIndex(localX, localY, windowWidth);
        var propagatedLevel = ApplyAttenuation(sourceLevel, blockingMask[windowIndex]);
        if (propagatedLevel == 0 || lightLevels[windowIndex] >= propagatedLevel)
        {
            return;
        }

        lightLevels[windowIndex] = propagatedLevel;
        propagationQueue.Enqueue((localX, localY));
    }

    private byte GetTileEmissiveLight(WorldTileCoord coord)
    {
        return _queryService.TryGetForegroundTileDef(coord, out var tileDef)
            ? tileDef.EmissiveLight
            : (byte)0;
    }

    private int ResolveSkySurfaceY(WorldGenerationContext generationContext, int worldX)
    {
        var generatorSurfaceY = _worldGenerator.GetSurfaceHeight(generationContext, worldX);
        if (_worldData.Metadata.MinTileY is { } minTileY)
        {
            return Math.Max(minTileY, generatorSurfaceY);
        }

        return generatorSurfaceY;
    }

    private byte ResolveDirectSkyLightLevel(
        int worldX,
        int surfaceY,
        int worldY,
        int localX,
        int windowOriginY,
        int windowWidth,
        IReadOnlyList<bool> blockingMask)
    {
        if (worldY <= surfaceY)
        {
            return MaxLightLevel;
        }

        var depthBelowSurface = worldY - surfaceY;
        if (depthBelowSurface > MaxLightLevel)
        {
            return 0;
        }

        byte currentLightLevel = MaxLightLevel;
        for (var scanWorldY = surfaceY + 1; scanWorldY <= worldY; scanWorldY++)
        {
            currentLightLevel = ApplyAttenuation(
                currentLightLevel,
                ResolveBlocksLight(worldX, scanWorldY, localX, windowOriginY, windowWidth, blockingMask));
            if (currentLightLevel == 0)
            {
                return 0;
            }
        }

        return currentLightLevel;
    }

    private bool ResolveBlocksLight(
        int worldX,
        int worldY,
        int localX,
        int windowOriginY,
        int windowWidth,
        IReadOnlyList<bool> blockingMask)
    {
        var scanLocalY = worldY - windowOriginY;
        if (scanLocalY >= 0 && scanLocalY < (blockingMask.Count / windowWidth))
        {
            return blockingMask[ToWindowIndex(localX, scanLocalY, windowWidth)];
        }

        return _queryService.BlocksLight(new WorldTileCoord(worldX, worldY));
    }

    private static byte ApplyAttenuation(byte lightLevel, bool passesThroughBlockingCell)
    {
        var attenuation = passesThroughBlockingCell ? 2 : 1;
        if (lightLevel <= attenuation)
        {
            return 0;
        }

        return (byte)(lightLevel - attenuation);
    }

    private static WorldTileCoord GetEntityCenterTile(Entity entity)
    {
        var worldBounds = entity.WorldBounds;
        var centerX = worldBounds.Left + (worldBounds.Width / 2f);
        var centerY = worldBounds.Top + (worldBounds.Height / 2f);
        return new WorldTileCoord((int)MathF.Floor(centerX), (int)MathF.Floor(centerY));
    }

    private static int ToWindowIndex(int localX, int localY, int windowWidth)
    {
        return (localY * windowWidth) + localX;
    }

    private static byte Max(byte left, byte right)
    {
        return left >= right ? left : right;
    }

    private readonly record struct DynamicLightState(WorldTileCoord Coord, byte LightLevel);
}
