using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;

namespace TileWorld.Engine.Tests.Storage;

public sealed class WorldMetadataSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundtripDefaultMetadata()
    {
        var serializer = new WorldMetadataSerializer();
        var metadata = new WorldMetadata();

        var json = serializer.Serialize(metadata);
        var restored = serializer.Deserialize(json);

        Assert.Equal(metadata.WorldId, restored.WorldId);
        Assert.Equal(metadata.Name, restored.Name);
        Assert.Equal(metadata.Seed, restored.Seed);
        Assert.Equal(metadata.WorldFormatVersion, restored.WorldFormatVersion);
        Assert.Equal(metadata.ChunkFormatVersion, restored.ChunkFormatVersion);
        Assert.Equal(metadata.GeneratorId, restored.GeneratorId);
        Assert.Equal(metadata.GeneratorVersion, restored.GeneratorVersion);
        Assert.Equal(metadata.WorldTime, restored.WorldTime);
        Assert.Equal(metadata.BoundsMode, restored.BoundsMode);
        Assert.Equal(metadata.SpawnTile, restored.SpawnTile);
        Assert.Equal(metadata.MinTileY, restored.MinTileY);
        Assert.Equal(metadata.MaxTileY, restored.MaxTileY);
        Assert.Equal(metadata.ChunkWidth, restored.ChunkWidth);
        Assert.Equal(metadata.ChunkHeight, restored.ChunkHeight);
    }

    [Fact]
    public void SerializeAndDeserialize_RoundtripExplicitMetadata()
    {
        var serializer = new WorldMetadataSerializer();
        var metadata = new WorldMetadata
        {
            WorldId = "world-5",
            Name = "Persisted World",
            Seed = 77,
            WorldFormatVersion = 3,
            ChunkFormatVersion = 2,
            GeneratorId = "flat_debug",
            GeneratorVersion = 4,
            WorldTime = 9001,
            BoundsMode = WorldBoundsMode.SemiInfinite,
            SpawnTile = new Int2(-10, 24),
            MinTileY = -128,
            MaxTileY = 384,
            ChunkWidth = 32,
            ChunkHeight = 32
        };

        var restored = serializer.Deserialize(serializer.Serialize(metadata));

        Assert.Equal(metadata.WorldId, restored.WorldId);
        Assert.Equal(metadata.Name, restored.Name);
        Assert.Equal(metadata.Seed, restored.Seed);
        Assert.Equal(metadata.WorldFormatVersion, restored.WorldFormatVersion);
        Assert.Equal(metadata.ChunkFormatVersion, restored.ChunkFormatVersion);
        Assert.Equal(metadata.GeneratorId, restored.GeneratorId);
        Assert.Equal(metadata.GeneratorVersion, restored.GeneratorVersion);
        Assert.Equal(metadata.WorldTime, restored.WorldTime);
        Assert.Equal(metadata.BoundsMode, restored.BoundsMode);
        Assert.Equal(metadata.SpawnTile, restored.SpawnTile);
        Assert.Equal(metadata.MinTileY, restored.MinTileY);
        Assert.Equal(metadata.MaxTileY, restored.MaxTileY);
    }

    [Fact]
    public void Deserialize_WithMismatchedChunkDimensionsThrows()
    {
        var serializer = new WorldMetadataSerializer();
        const string json = """
            {
              "worldId": "world-1",
              "name": "Broken",
              "seed": 1,
              "worldFormatVersion": 1,
              "chunkFormatVersion": 1,
              "worldTime": 0,
              "boundsMode": 0,
              "spawnTile": { "x": 0, "y": 0 },
              "chunkWidth": 16,
              "chunkHeight": 32
            }
            """;

        Assert.Throws<InvalidDataException>(() => serializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_WithInvalidVerticalBoundsThrows()
    {
        var serializer = new WorldMetadataSerializer();
        const string json = """
            {
              "worldId": "world-1",
              "name": "Broken Bounds",
              "seed": 1,
              "worldFormatVersion": 2,
              "chunkFormatVersion": 2,
              "generatorId": "overworld",
              "generatorVersion": 1,
              "worldTime": 0,
              "boundsMode": 0,
              "spawnTile": { "x": 0, "y": 0 },
              "minTileY": 32,
              "maxTileY": 16,
              "chunkWidth": 32,
              "chunkHeight": 32
            }
            """;

        Assert.Throws<ArgumentException>(() => serializer.Deserialize(json));
    }
}
