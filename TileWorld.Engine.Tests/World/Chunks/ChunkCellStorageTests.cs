using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;

namespace TileWorld.Engine.Tests.World.Chunks;

public sealed class ChunkCellStorageTests
{
    [Fact]
    public void Count_MatchesChunkCellCount()
    {
        var storage = new ChunkCellStorage();

        Assert.Equal(ChunkDimensions.CellCount, storage.Count);
    }

    [Fact]
    public void SetAndGetCell_WorksAcrossEntireChunkSurface()
    {
        var storage = new ChunkCellStorage();

        for (var y = 0; y < ChunkDimensions.Height; y++)
        {
            for (var x = 0; x < ChunkDimensions.Width; x++)
            {
                storage.SetCell(x, y, new TileCell
                {
                    ForegroundTileId = (ushort)(x + y),
                    Variant = (ushort)(x * y)
                });
            }
        }

        for (var y = 0; y < ChunkDimensions.Height; y++)
        {
            for (var x = 0; x < ChunkDimensions.Width; x++)
            {
                var cell = storage.GetCell(x, y);

                Assert.Equal((ushort)(x + y), cell.ForegroundTileId);
                Assert.Equal((ushort)(x * y), cell.Variant);
            }
        }
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(32, 0)]
    [InlineData(0, 32)]
    public void OutOfBoundsAccess_ThrowsArgumentOutOfRangeException(int localX, int localY)
    {
        var storage = new ChunkCellStorage();

        Assert.Throws<ArgumentOutOfRangeException>(() => storage.GetCell(localX, localY));
        Assert.Throws<ArgumentOutOfRangeException>(() => storage.SetCell(localX, localY, TileCell.Empty));
    }
}
