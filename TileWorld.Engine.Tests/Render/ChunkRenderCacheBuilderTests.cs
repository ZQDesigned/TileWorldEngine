using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Render;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Render;

public sealed class ChunkRenderCacheBuilderTests
{
    [Fact]
    public void Build_ForEmptyChunkProducesNoForegroundCommands()
    {
        var builder = CreateBuilder();
        var chunk = new Chunk(new ChunkCoord(1, -1));

        var cache = builder.Build(chunk, 1);

        Assert.True(cache.IsBuilt);
        Assert.Empty(cache.ForegroundCommands);
        Assert.Equal(new RectI(512, -512, 512, 512), cache.WorldPixelBounds);
    }

    [Fact]
    public void Build_ForTileWithVariantStripUsesVariantOffset()
    {
        var registry = new ContentRegistry();
        registry.RegisterTile(new TileDef
        {
            Id = 1,
            Name = "VariantStone",
            Category = "Terrain",
            Visual = new TileVisualDef(
                "debug/white",
                new RectI(2, 4, 16, 16),
                new ColorRgba32(120, 120, 120),
                true)
        });

        var builder = new ChunkRenderCacheBuilder(registry, new WorldRenderSettings());
        var chunk = new Chunk(new ChunkCoord(0, 0));
        chunk.SetCell(3, 4, new TileCell
        {
            ForegroundTileId = 1,
            Variant = 3
        });

        var cache = builder.Build(chunk, 7);
        var command = Assert.Single(cache.ForegroundCommands);

        Assert.Equal(7, cache.LastBuildTick);
        Assert.Equal("debug/white", command.TextureKey);
        Assert.Equal(new RectI(50, 4, 16, 16), command.SourceRect);
        Assert.Equal(new RectI(48, 64, 16, 16), command.DestinationRectPixels);
        Assert.Equal(new ColorRgba32(120, 120, 120), command.Tint);
    }

    [Fact]
    public void Build_ForBackgroundWallProducesBackgroundCommand()
    {
        var registry = new ContentRegistry();
        registry.RegisterTile(new TileDef
        {
            Id = 2,
            Name = "Stone",
            Category = "Terrain",
            Visual = new TileVisualDef(
                "debug/white",
                new RectI(0, 0, 1, 1),
                new ColorRgba32(130, 130, 130),
                false)
        });
        registry.RegisterWall(new WallDef
        {
            Id = 1,
            Name = "Stone Wall",
            Visual = new TileVisualDef(
                "debug/white",
                new RectI(0, 0, 1, 1),
                new ColorRgba32(90, 90, 90, 160),
                false)
        });

        var builder = new ChunkRenderCacheBuilder(registry, new WorldRenderSettings());
        var chunk = new Chunk(new ChunkCoord(0, 0));
        chunk.SetCell(2, 3, new TileCell
        {
            BackgroundWallId = 1,
            ForegroundTileId = 2
        });

        var cache = builder.Build(chunk, 1);
        var command = Assert.Single(cache.BackgroundCommands);
        var foregroundCommand = Assert.Single(cache.ForegroundCommands);

        Assert.Equal(new RectI(32, 48, 16, 16), command.DestinationRectPixels);
        Assert.Equal(new ColorRgba32(90, 90, 90, 160), command.Tint);
        Assert.InRange(command.LayerDepth, 0f, 1f);
        Assert.True(command.LayerDepth < foregroundCommand.LayerDepth);
    }

    private static ChunkRenderCacheBuilder CreateBuilder()
    {
        return new ChunkRenderCacheBuilder(new ContentRegistry(), new WorldRenderSettings());
    }
}
