using TileWorld.Engine.Hosting;

namespace TileWorld.Engine.Tests.Hosting;

public sealed class FrameTimeTests
{
    [Fact]
    public void Constructor_AssignsAllValues()
    {
        var frameTime = new FrameTime(TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(16), true);

        Assert.Equal(TimeSpan.FromSeconds(10), frameTime.TotalTime);
        Assert.Equal(TimeSpan.FromMilliseconds(16), frameTime.ElapsedTime);
        Assert.True(frameTime.IsFixedStep);
    }

    [Fact]
    public void ValueEquality_IsStable()
    {
        var first = new FrameTime(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(33), false);
        var second = new FrameTime(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(33), false);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
