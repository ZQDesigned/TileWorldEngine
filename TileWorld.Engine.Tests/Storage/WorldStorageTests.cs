using TileWorld.Engine.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Storage;

public sealed class WorldStorageTests
{
    [Fact]
    public void SaveAndLoad_MetadataAndChunks_UseExpectedPaths()
    {
        using var directory = new TestDirectoryScope();
        var storage = new WorldStorage();
        var metadata = new WorldMetadata
        {
            WorldId = "world-storage",
            Name = "Storage Test"
        };
        var chunk = new Chunk(new ChunkCoord(1, -1));
        chunk.SetCell(2, 3, new TileCell { ForegroundTileId = 9, Variant = 4 });

        storage.SaveMetadata(directory.Path, metadata);
        storage.SaveChunk(directory.Path, chunk);

        var loadedMetadata = storage.LoadMetadata(directory.Path);
        var loadedChunk = storage.TryLoadChunk(directory.Path, new ChunkCoord(1, -1));

        Assert.True(storage.HasWorld(directory.Path));
        Assert.True(storage.HasChunkData(directory.Path, new ChunkCoord(1, -1)));
        Assert.Equal(metadata.WorldId, loadedMetadata.WorldId);
        Assert.Equal(metadata.Name, loadedMetadata.Name);
        Assert.NotNull(loadedChunk);
        Assert.Equal((ushort)9, loadedChunk.GetCell(2, 3).ForegroundTileId);
        Assert.Equal((ushort)4, loadedChunk.GetCell(2, 3).Variant);
    }

    [Fact]
    public void TryLoadChunk_WhenChunkDataDoesNotExist_ReturnsNull()
    {
        using var directory = new TestDirectoryScope();
        var storage = new WorldStorage();

        Assert.Null(storage.TryLoadChunk(directory.Path, new ChunkCoord(0, 0)));
        Assert.False(storage.HasChunkData(directory.Path, new ChunkCoord(0, 0)));
    }
}
