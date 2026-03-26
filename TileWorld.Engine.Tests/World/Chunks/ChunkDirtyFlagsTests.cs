using TileWorld.Engine.World.Chunks;

namespace TileWorld.Engine.Tests.World.Chunks;

public sealed class ChunkDirtyFlagsTests
{
    [Fact]
    public void None_IsTheZeroValue()
    {
        Assert.Equal(0, (int)ChunkDirtyFlags.None);
    }

    [Fact]
    public void Flags_CanBeCombinedAndQueried()
    {
        var flags = ChunkDirtyFlags.RenderDirty | ChunkDirtyFlags.SaveDirty | ChunkDirtyFlags.AutoTileDirty;

        Assert.True(flags.HasFlag(ChunkDirtyFlags.RenderDirty));
        Assert.True(flags.HasFlag(ChunkDirtyFlags.SaveDirty));
        Assert.True(flags.HasFlag(ChunkDirtyFlags.AutoTileDirty));
        Assert.False(flags.HasFlag(ChunkDirtyFlags.LightDirty));
    }

    [Fact]
    public void BitwiseAnd_CanCheckMasks()
    {
        var flags = ChunkDirtyFlags.CollisionDirty | ChunkDirtyFlags.LiquidDirty;

        Assert.NotEqual(ChunkDirtyFlags.None, flags & ChunkDirtyFlags.CollisionDirty);
        Assert.Equal(ChunkDirtyFlags.None, flags & ChunkDirtyFlags.SaveDirty);
    }
}
