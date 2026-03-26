using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.World.Chunks;

public sealed class ChunkTests
{
    [Fact]
    public void Constructor_SetsDefaultRuntimeState()
    {
        var chunk = new Chunk(new ChunkCoord(2, -3));

        Assert.Equal(new ChunkCoord(2, -3), chunk.Coord);
        Assert.Equal(ChunkState.Loaded, chunk.State);
        Assert.Equal(ChunkDirtyFlags.None, chunk.DirtyFlags);
    }

    [Fact]
    public void SetCellAndGetCell_DelegateToStorage()
    {
        var chunk = new Chunk(new ChunkCoord(0, 0));
        var expectedCell = new TileCell { ForegroundTileId = 21, BackgroundWallId = 9, Variant = 3 };

        chunk.SetCell(5, 7, expectedCell);

        Assert.Equal(expectedCell, chunk.GetCell(5, 7));
        Assert.Equal((7 * ChunkDimensions.Width) + 5, chunk.ToIndex(5, 7));
    }
}
