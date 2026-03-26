using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.World.Model;

public sealed class WorldDataTests
{
    [Fact]
    public void GetOrCreateChunk_IsIdempotentPerCoordinate()
    {
        var worldData = new WorldData(new WorldMetadata { Name = "Test World" });

        var first = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        var second = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));

        Assert.Same(first, second);
        Assert.Equal(1, worldData.LoadedChunkCount);
    }

    [Fact]
    public void SetChunk_MakesChunkAvailableForLookup()
    {
        var worldData = new WorldData(new WorldMetadata());
        var chunk = new Chunk(new ChunkCoord(-2, 4));

        worldData.SetChunk(chunk);

        Assert.True(worldData.TryGetChunk(chunk.Coord, out var queriedChunk));
        Assert.Same(chunk, queriedChunk);
        Assert.True(worldData.HasChunk(chunk.Coord));
    }

    [Fact]
    public void EnumerateLoadedChunks_ReturnsAllDistinctChunks()
    {
        var worldData = new WorldData(new WorldMetadata());

        worldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        worldData.GetOrCreateChunk(new ChunkCoord(1, 0));
        worldData.GetOrCreateChunk(new ChunkCoord(0, 1));

        var loadedChunks = worldData.EnumerateLoadedChunks().ToArray();

        Assert.Equal(3, loadedChunks.Length);
        Assert.Contains(loadedChunks, chunk => chunk.Coord == new ChunkCoord(1, 0));
        Assert.Contains(loadedChunks, chunk => chunk.Coord == new ChunkCoord(0, 1));
    }
}
