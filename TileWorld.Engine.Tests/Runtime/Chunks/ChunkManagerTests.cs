using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

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

        var resolvedChunk = chunkManager.GetOrLoadChunk(new ChunkCoord(0, 0));

        Assert.Same(existingChunk, resolvedChunk);
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

        var loadedChunk = chunkManager.GetOrLoadChunk(new ChunkCoord(1, 2));

        Assert.Equal((ushort)6, loadedChunk.GetCell(4, 5).ForegroundTileId);
        Assert.Equal((ushort)3, loadedChunk.GetCell(4, 5).Variant);
        Assert.True((loadedChunk.DirtyFlags & ChunkDirtyFlags.RenderDirty) != ChunkDirtyFlags.None);
    }

    [Fact]
    public void GetOrLoadChunk_CreatesNewChunkWhenStorageIsMissing()
    {
        using var directory = new TileWorld.Engine.Tests.Storage.TestDirectoryScope();
        var chunkManager = new ChunkManager(new WorldData(new WorldMetadata()), new WorldStorage(), directory.Path);

        var chunk = chunkManager.GetOrLoadChunk(new ChunkCoord(-1, -2));

        Assert.Equal(new ChunkCoord(-1, -2), chunk.Coord);
        Assert.False((chunk.DirtyFlags & ChunkDirtyFlags.SaveDirty) != ChunkDirtyFlags.None);
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
}
