using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.World.Coordinates;

public sealed class CoordinateTests
{
    [Fact]
    public void WorldTileCoord_UsesValueEquality()
    {
        var first = new WorldTileCoord(12, -6);
        var second = new WorldTileCoord(12, -6);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void WorldTileCoord_OffsetReturnsNewValue()
    {
        var coord = new WorldTileCoord(7, 8);

        Assert.Equal(new WorldTileCoord(4, 10), coord.Offset(-3, 2));
    }

    [Fact]
    public void WorldTileCoord_ToStringIsStable()
    {
        Assert.Equal("WorldTileCoord(1, 2)", new WorldTileCoord(1, 2).ToString());
    }

    [Fact]
    public void ChunkCoord_UsesValueEquality()
    {
        var first = new ChunkCoord(-2, 9);
        var second = new ChunkCoord(-2, 9);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ChunkCoord_OffsetReturnsNewValue()
    {
        var coord = new ChunkCoord(3, -5);

        Assert.Equal(new ChunkCoord(4, -7), coord.Offset(1, -2));
    }

    [Fact]
    public void ChunkCoord_ToStringIsStable()
    {
        Assert.Equal("ChunkCoord(3, -9)", new ChunkCoord(3, -9).ToString());
    }
}
