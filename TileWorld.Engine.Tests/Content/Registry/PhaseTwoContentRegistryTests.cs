using TileWorld.Engine.Content.Items;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Tests.Content.Registry;

public sealed class PhaseTwoContentRegistryTests
{
    [Fact]
    public void Constructor_RegistersDefaultNoWallDefinition()
    {
        var registry = new ContentRegistry();

        var wall = registry.GetWallDef(0);

        Assert.Equal((ushort)0, wall.Id);
        Assert.Equal("NoWall", wall.Name);
        Assert.False(wall.CountsAsRoomWall);
    }

    [Fact]
    public void RegisterObjectAndItem_MakesDefinitionsQueryable()
    {
        var registry = new ContentRegistry();
        var objectDef = new ObjectDef
        {
            Id = 100,
            Name = "Crate",
            SizeInTiles = new Int2(2, 2),
            RequiresSupport = true
        };
        var itemDef = new ItemDef
        {
            Id = 900,
            Name = "Crate Item",
            PlaceObjectDefId = 100
        };
        var wallDef = new WallDef
        {
            Id = 7,
            Name = "Stone Wall",
            CountsAsRoomWall = true,
            BreakDropItemId = 901
        };

        registry.RegisterObject(objectDef);
        registry.RegisterItem(itemDef);
        registry.RegisterWall(wallDef);

        Assert.Same(objectDef, registry.GetObjectDef(100));
        Assert.Same(itemDef, registry.GetItemDef(900));
        Assert.Same(wallDef, registry.GetWallDef(7));
        Assert.Equal(100, registry.GetItemDef(900).PlaceObjectDefId);
        Assert.Equal(901, registry.GetWallDef(7).BreakDropItemId);
    }
}
