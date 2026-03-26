using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Tests.Core.Math;

public sealed class RectITests
{
    [Fact]
    public void Boundaries_AreDerivedFromPositionAndSize()
    {
        var rect = new RectI(10, 20, 30, 40);

        Assert.Equal(10, rect.Left);
        Assert.Equal(20, rect.Top);
        Assert.Equal(40, rect.Right);
        Assert.Equal(60, rect.Bottom);
    }

    [Fact]
    public void Contains_UsesInclusiveLeftTopAndExclusiveRightBottom()
    {
        var rect = new RectI(2, 3, 4, 5);

        Assert.True(rect.Contains(2, 3));
        Assert.True(rect.Contains(new Int2(5, 7)));
        Assert.False(rect.Contains(6, 7));
        Assert.False(rect.Contains(5, 8));
    }

    [Fact]
    public void ZeroSizedRectangles_AreAllowedButContainNothing()
    {
        var rect = new RectI(4, 5, 0, 0);

        Assert.Equal(4, rect.Right);
        Assert.Equal(5, rect.Bottom);
        Assert.False(rect.Contains(4, 5));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void NegativeSize_ThrowsArgumentOutOfRangeException(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RectI(0, 0, width, height));
    }
}
