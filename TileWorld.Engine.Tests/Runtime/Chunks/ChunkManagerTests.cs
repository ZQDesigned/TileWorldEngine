using TileWorld.Engine.Content.Biomes;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Generation;

namespace TileWorld.Engine.Tests.Runtime.Chunks;

public sealed class ChunkManagerTests
{
    [Fact]
    public void GetOrLoadChunk_ReturnsAlreadyLoadedChunk()
    {
        using var directory = new TileWorld.Engine.Tests.Storage.TestDirectoryScope();
        var worldData = new WorldData(new WorldMetadata());
        var existingChunk = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        var chunkManager = new ChunkManager(worldData, new WorldStorage(), directory.Path);

        var resolvedChunk = chunkManager.GetOrLoadChunkDetailed(new ChunkCoord(0, 0));

        Assert.Same(existingChunk, resolvedChunk.Chunk);
        Assert.Equal(ChunkLoadSource.Memory, resolvedChunk.Source);
    }

    [Fact]
    public void GetOrLoadChunk_LoadsChunkFromDiskWhenPresent()
    {
        using var directory = new TileWorld.Engine.Tests.Storage.TestDirectoryScope();
        var storage = new WorldStorage();
        storage.SaveMetadata(directory.Path, new WorldMetadata());

        var savedChunk = new Chunk(new ChunkCoord(1, 2));
        savedChunk.SetCell(4, 5, new TileCell { ForegroundTileId = 6, Variant = 3 });
        storage.SaveChunk(directory.Path, savedChunk);

        var worldData = new WorldData(new WorldMetadata());
        var chunkManager = new ChunkManager(worldData, storage, directory.Path);

        var loadedChunk = chunkManager.GetOrLoadChunkDetailed(new ChunkCoord(1, 2));

        Assert.Equal(ChunkLoadSource.Disk, loadedChunk.Source);
        Assert.Equal((ushort)6, loadedChunk.Chunk.GetCell(4, 5).ForegroundTileId);
        Assert.Equal((ushort)3, loadedChunk.Chunk.GetCell(4, 5).Variant);
        Assert.True((loadedChunk.Chunk.DirtyFlags & ChunkDirtyFlags.RenderDirty) != ChunkDirtyFlags.None);
    }

    [Fact]
    public void GetOrLoadChunk_GeneratesChunkWhenStorageIsMissingAndGeneratorExists()
    {
        using var directory = new TileWorld.Engine.Tests.Storage.TestDirectoryScope();
        var metadata = new WorldMetadata
        {
            WorldId = "generated-world",
            Name = "Generated World",
            Seed = 1234,
            SpawnTile = new TileWorld.Engine.Core.Math.Int2(4, 18)
        };
        var chunkManager = new ChunkManager(
            new WorldData(metadata),
            new WorldStorage(),
            directory.Path,
            CreateRegistry(),
            new FlatDebugWorldGenerator());

        var result = chunkManager.GetOrLoadChunkDetailed(new ChunkCoord(-1, -2));

        Assert.Equal(ChunkLoadSource.Generated, result.Source);
        Assert.Equal(new ChunkCoord(-1, -2), result.Chunk.Coord);
        Assert.True((result.Chunk.DirtyFlags & ChunkDirtyFlags.SaveDirty) != ChunkDirtyFlags.None);
    }

    [Fact]
    public void GetOrLoadChunk_CreatesEmptyChunkWhenStorageIsMissingAndNoGeneratorExists()
    {
        using var directory = new TileWorld.Engine.Tests.Storage.TestDirectoryScope();
        var chunkManager = new ChunkManager(new WorldData(new WorldMetadata()), new WorldStorage(), directory.Path);

        var result = chunkManager.GetOrLoadChunkDetailed(new ChunkCoord(-1, -2));

        Assert.Equal(ChunkLoadSource.EmptyCreated, result.Source);
        Assert.Equal(new ChunkCoord(-1, -2), result.Chunk.Coord);
        Assert.False((result.Chunk.DirtyFlags & ChunkDirtyFlags.SaveDirty) != ChunkDirtyFlags.None);
    }

    [Fact]
    public void SaveDirtyChunks_PersistsAndClearsSaveDirty()
    {
        using var directory = new TileWorld.Engine.Tests.Storage.TestDirectoryScope();
        var storage = new WorldStorage();
        storage.SaveMetadata(directory.Path, new WorldMetadata());
        var worldData = new WorldData(new WorldMetadata());
        var chunkManager = new ChunkManager(worldData, storage, directory.Path);
        var chunk = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        chunk.SetCell(1, 1, new TileCell { ForegroundTileId = 8 });
        chunk.DirtyFlags |= ChunkDirtyFlags.SaveDirty;

        chunkManager.SaveDirtyChunks();

        var loadedChunk = storage.TryLoadChunk(directory.Path, new ChunkCoord(0, 0));
        Assert.NotNull(loadedChunk);
        Assert.False((chunk.DirtyFlags & ChunkDirtyFlags.SaveDirty) != ChunkDirtyFlags.None);
        Assert.Equal((ushort)8, loadedChunk.GetCell(1, 1).ForegroundTileId);
    }

    [Fact]
    public void EnsureActiveAround_QueuesOuterRingPrefetch()
    {
        using var directory = new TileWorld.Engine.Tests.Storage.TestDirectoryScope();
        var queuedEvents = new List<ChunkQueuedEvent>();
        var eventBus = new WorldEventBus();
        eventBus.Subscribe<ChunkQueuedEvent>(queuedEvents.Add);

        var chunkManager = new ChunkManager(
            new WorldData(new WorldMetadata { Seed = 321, SpawnTile = new TileWorld.Engine.Core.Math.Int2(4, 18) }),
            new WorldStorage(),
            directory.Path,
            CreateRegistry(),
            new FlatDebugWorldGenerator(),
            activeRadiusInChunks: 1,
            eventBus);

        chunkManager.EnsureActiveAround(new WorldTileCoord(0, 0));

        Assert.NotEmpty(queuedEvents);
    }

    private static ContentRegistry CreateRegistry()
    {
        var registry = new ContentRegistry();
        registry.RegisterTile(new TileDef
        {
            Id = 1,
            Name = "Stone",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1
        });
        registry.RegisterTile(new TileDef
        {
            Id = 2,
            Name = "Dirt",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1
        });
        registry.RegisterWall(new WallDef
        {
            Id = 1,
            Name = "Stone Wall"
        });
        registry.RegisterWall(new WallDef
        {
            Id = 2,
            Name = "Dirt Wall"
        });
        registry.RegisterBiome(new BiomeDef
        {
            Id = 1,
            Name = "Plains",
            SurfaceTileId = 2,
            SubsurfaceTileId = 1,
            SurfaceWallId = 2,
            Priority = 5
        });
        registry.RegisterBiome(new BiomeDef
        {
            Id = 2,
            Name = "Rocky",
            SurfaceTileId = 1,
            SubsurfaceTileId = 1,
            SurfaceWallId = 1,
            Priority = 10
        });

        return registry;
    }
}
