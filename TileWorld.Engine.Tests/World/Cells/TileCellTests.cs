using TileWorld.Engine.World.Cells;

namespace TileWorld.Engine.Tests.World.Cells;

public sealed class TileCellTests
{
    [Fact]
    public void Empty_MatchesDefaultValue()
    {
        Assert.Equal(default(TileCell), TileCell.Empty);
    }

    [Fact]
    public void Properties_RetainAssignedValues()
    {
        var cell = new TileCell
        {
            ForegroundTileId = 12,
            BackgroundWallId = 4,
            LiquidType = 2,
            LiquidAmount = 128,
            Variant = 7,
            Flags = 15
        };

        Assert.Equal((ushort)12, cell.ForegroundTileId);
        Assert.Equal((ushort)4, cell.BackgroundWallId);
        Assert.Equal((byte)2, cell.LiquidType);
        Assert.Equal((byte)128, cell.LiquidAmount);
        Assert.Equal((ushort)7, cell.Variant);
        Assert.Equal((ushort)15, cell.Flags);
    }
}
