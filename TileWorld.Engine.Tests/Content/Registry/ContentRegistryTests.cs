using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;

namespace TileWorld.Engine.Tests.Content.Registry;

public sealed class ContentRegistryTests
{
    [Fact]
    public void Constructor_RegistersAirTile()
    {
        var registry = new ContentRegistry();

        var airTile = registry.GetTileDef(0);

        Assert.Equal((ushort)0, airTile.Id);
        Assert.Equal("Air", airTile.Name);
        Assert.True(registry.HasTileDef(0));
        Assert.Equal("debug/white", airTile.Visual.TextureKey);
        Assert.Equal(new(0, 0, 1, 1), airTile.Visual.SourceRect);
    }

    [Fact]
    public void RegisterTile_MakesTileQueryable()
    {
        var registry = new ContentRegistry();
        var tileDef = new TileDef
        {
            Id = 1,
            Name = "Stone",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 5,
            AutoTileGroupId = 1
        };

        registry.RegisterTile(tileDef);

        Assert.Same(tileDef, registry.GetTileDef(1));
        Assert.True(registry.TryGetTileDef(1, out var queriedTile));
        Assert.Same(tileDef, queriedTile);
    }

    [Fact]
    public void RegisterTile_WithDuplicateIdThrows()
    {
        var registry = new ContentRegistry();

        registry.RegisterTile(new TileDef { Id = 1, Name = "Stone", Category = "Terrain" });

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterTile(new TileDef { Id = 1, Name = "Dirt", Category = "Terrain" }));
    }
}
