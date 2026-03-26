using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Tests.Core.Math;

public sealed class Int2Tests
{
    [Fact]
    public void DefaultValue_MatchesZero()
    {
        Assert.Equal(Int2.Zero, default(Int2));
    }

    [Fact]
    public void Constructor_SetsCoordinates()
    {
        var value = new Int2(3, -7);

        Assert.Equal(3, value.X);
        Assert.Equal(-7, value.Y);
    }

    [Fact]
    public void Equality_UsesValueSemantics()
    {
        var first = new Int2(5, 9);
        var second = new Int2(5, 9);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Operators_AddAndSubtractCoordinates()
    {
        var first = new Int2(10, 4);
        var second = new Int2(-3, 8);

        Assert.Equal(new Int2(7, 12), first + second);
        Assert.Equal(new Int2(13, -4), first - second);
    }
}
