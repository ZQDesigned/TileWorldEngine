using TileWorld.Engine.Storage;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Objects;

namespace TileWorld.Engine.Tests.Storage;

public sealed class ChunkSerializerObjectPayloadTests
{
    [Fact]
    public void SerializePayload_RoundtripsAnchoredObjects()
    {
        var serializer = new ChunkSerializer();
        var chunk = new Chunk(new ChunkCoord(1, -1));
        var anchoredObjects = new[]
        {
            new ObjectInstance
            {
                InstanceId = 12,
                ObjectDefId = 100,
                AnchorCoord = new WorldTileCoord(32, -30),
                Direction = Direction.Right,
                StateFlags = 5
            }
        };

        var restored = serializer.DeserializePayload(serializer.Serialize(chunk, anchoredObjects), new ChunkCoord(1, -1));

        var restoredObject = Assert.Single(restored.AnchoredObjects);
        Assert.Equal(12, restoredObject.InstanceId);
        Assert.Equal(100, restoredObject.ObjectDefId);
        Assert.Equal(new WorldTileCoord(32, -30), restoredObject.AnchorCoord);
        Assert.Equal(Direction.Right, restoredObject.Direction);
        Assert.Equal((ushort)5, restoredObject.StateFlags);
    }
}
