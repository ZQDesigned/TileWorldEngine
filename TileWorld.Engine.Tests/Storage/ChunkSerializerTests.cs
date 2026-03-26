using TileWorld.Engine.Storage;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Storage;

public sealed class ChunkSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundtripChunkDataWithoutRuntimeState()
    {
        var serializer = new ChunkSerializer();
        var chunk = new Chunk(new ChunkCoord(-2, 3))
        {
            State = ChunkState.Active,
            DirtyFlags = ChunkDirtyFlags.RenderDirty | ChunkDirtyFlags.SaveDirty
        };
        chunk.SetCell(0, 0, new TileCell
        {
            ForegroundTileId = 5,
            BackgroundWallId = 2,
            LiquidType = 1,
            LiquidAmount = 255,
            Variant = 7,
            Flags = 9
        });

        var restored = serializer.Deserialize(serializer.Serialize(chunk), new ChunkCoord(-2, 3));
        var restoredCell = restored.GetCell(0, 0);

        Assert.Equal(new ChunkCoord(-2, 3), restored.Coord);
        Assert.Equal(ChunkState.Loaded, restored.State);
        Assert.Equal(ChunkDirtyFlags.None, restored.DirtyFlags);
        Assert.Equal((ushort)5, restoredCell.ForegroundTileId);
        Assert.Equal((ushort)2, restoredCell.BackgroundWallId);
        Assert.Equal((byte)1, restoredCell.LiquidType);
        Assert.Equal((byte)255, restoredCell.LiquidAmount);
        Assert.Equal((ushort)7, restoredCell.Variant);
        Assert.Equal((ushort)9, restoredCell.Flags);
    }

    [Fact]
    public void Deserialize_WithInvalidMagicThrows()
    {
        var serializer = new ChunkSerializer();

        Assert.Throws<InvalidDataException>(() =>
            serializer.Deserialize([1, 2, 3, 4], new ChunkCoord(0, 0)));
    }

    [Fact]
    public void Deserialize_WithUnexpectedCoordThrows()
    {
        var serializer = new ChunkSerializer();
        var chunk = new Chunk(new ChunkCoord(1, 1));
        var data = serializer.Serialize(chunk);

        Assert.Throws<InvalidDataException>(() =>
            serializer.Deserialize(data, new ChunkCoord(2, 1)));
    }

    [Fact]
    public void Deserialize_WithTruncatedDataThrows()
    {
        var serializer = new ChunkSerializer();
        var chunk = new Chunk(new ChunkCoord(0, 0));
        var data = serializer.Serialize(chunk);
        Array.Resize(ref data, data.Length - 5);

        Assert.Throws<InvalidDataException>(() =>
            serializer.Deserialize(data, new ChunkCoord(0, 0)));
    }
}
