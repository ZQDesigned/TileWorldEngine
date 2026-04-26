using TileWorld.Engine.Core.Math;
using TileWorld.Engine.World;

namespace TileWorld.Engine.Tests.World.Model;

public sealed class WorldMetadataTests
{
    [Fact]
    public void DefaultValues_MatchRuntimeMetadataBaseline()
    {
        var metadata = new WorldMetadata();

        Assert.Equal(string.Empty, metadata.WorldId);
        Assert.Equal(string.Empty, metadata.Name);
        Assert.Equal(0, metadata.Seed);
        Assert.Equal(2, metadata.WorldFormatVersion);
        Assert.Equal(2, metadata.ChunkFormatVersion);
        Assert.Equal(string.Empty, metadata.GeneratorId);
        Assert.Equal(1, metadata.GeneratorVersion);
        Assert.Equal(0L, metadata.WorldTime);
        Assert.Equal(WorldBoundsMode.LargeFinite, metadata.BoundsMode);
        Assert.Equal(Int2.Zero, metadata.SpawnTile);
        Assert.Null(metadata.MinTileY);
        Assert.Null(metadata.MaxTileY);
        Assert.Equal(32, metadata.ChunkWidth);
        Assert.Equal(32, metadata.ChunkHeight);
    }

    [Fact]
    public void InitProperties_AcceptExplicitValues()
    {
        var metadata = new WorldMetadata
        {
            WorldId = "world-001",
            Name = "Sandbox",
            Seed = 42,
            WorldFormatVersion = 3,
            ChunkFormatVersion = 2,
            GeneratorId = "flat_debug",
            GeneratorVersion = 7,
            WorldTime = 99,
            BoundsMode = WorldBoundsMode.SemiInfinite,
            SpawnTile = new Int2(10, 12),
            MinTileY = -64,
            MaxTileY = 256,
            ChunkWidth = 32,
            ChunkHeight = 32
        };

        Assert.Equal("world-001", metadata.WorldId);
        Assert.Equal("Sandbox", metadata.Name);
        Assert.Equal(42, metadata.Seed);
        Assert.Equal(3, metadata.WorldFormatVersion);
        Assert.Equal(2, metadata.ChunkFormatVersion);
        Assert.Equal("flat_debug", metadata.GeneratorId);
        Assert.Equal(7, metadata.GeneratorVersion);
        Assert.Equal(99L, metadata.WorldTime);
        Assert.Equal(WorldBoundsMode.SemiInfinite, metadata.BoundsMode);
        Assert.Equal(new Int2(10, 12), metadata.SpawnTile);
        Assert.Equal(-64, metadata.MinTileY);
        Assert.Equal(256, metadata.MaxTileY);
        Assert.Equal(32, metadata.ChunkWidth);
        Assert.Equal(32, metadata.ChunkHeight);
    }
}
