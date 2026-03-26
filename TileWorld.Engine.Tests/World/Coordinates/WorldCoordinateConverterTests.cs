using TileWorld.Engine.Core.Math;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.World.Coordinates;

public sealed class WorldCoordinateConverterTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(31, 31, 0, 0, 31, 31)]
    [InlineData(32, 32, 1, 1, 0, 0)]
    [InlineData(-1, -1, -1, -1, 31, 31)]
    [InlineData(-32, -32, -1, -1, 0, 0)]
    [InlineData(-33, -33, -2, -2, 31, 31)]
    public void WorldToChunkAndLocalCoordinateConversion_UsesFloorSemantics(
        int worldX,
        int worldY,
        int expectedChunkX,
        int expectedChunkY,
        int expectedLocalX,
        int expectedLocalY)
    {
        var coord = new WorldTileCoord(worldX, worldY);

        Assert.Equal(new ChunkCoord(expectedChunkX, expectedChunkY), WorldCoordinateConverter.ToChunkCoord(coord));
        Assert.Equal(new Int2(expectedLocalX, expectedLocalY), WorldCoordinateConverter.ToLocalCoord(coord));
    }

    [Fact]
    public void ToChunkOrigin_ReturnsChunkWorldStart()
    {
        var origin = WorldCoordinateConverter.ToChunkOrigin(new ChunkCoord(1, -2));

        Assert.Equal(new WorldTileCoord(32, -64), origin);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(31, 0, 31)]
    [InlineData(0, 31, 992)]
    [InlineData(31, 31, 1023)]
    public void ToIndex_MapsChunkCornersCorrectly(int localX, int localY, int expectedIndex)
    {
        Assert.Equal(expectedIndex, WorldCoordinateConverter.ToIndex(localX, localY));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(32, 0)]
    [InlineData(0, 32)]
    public void ToIndex_WithOutOfBoundsCoordinatesThrows(int localX, int localY)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WorldCoordinateConverter.ToIndex(localX, localY));
    }

    [Fact]
    public void IsInsideLocal_MatchesChunkBounds()
    {
        Assert.True(WorldCoordinateConverter.IsInsideLocal(0, 0));
        Assert.True(WorldCoordinateConverter.IsInsideLocal(31, 31));
        Assert.False(WorldCoordinateConverter.IsInsideLocal(-1, 0));
        Assert.False(WorldCoordinateConverter.IsInsideLocal(0, 32));
    }
}
