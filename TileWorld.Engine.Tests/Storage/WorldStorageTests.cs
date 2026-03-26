using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Storage;

public sealed class WorldStorageTests
{
    [Fact]
    public void SaveAndLoad_MetadataAndChunks_UseExpectedPaths()
    {
        using var directory = new TestDirectoryScope();
        var storage = new WorldStorage();
        var metadata = new WorldMetadata
        {
            WorldId = "world-storage",
            Name = "Storage Test"
        };
        var chunk = new Chunk(new ChunkCoord(1, -1));
        chunk.SetCell(2, 3, new TileCell { ForegroundTileId = 9, Variant = 4 });

        storage.SaveMetadata(directory.Path, metadata);
        storage.SaveChunk(directory.Path, chunk);

        var loadedMetadata = storage.LoadMetadata(directory.Path);
        var loadedChunk = storage.TryLoadChunk(directory.Path, new ChunkCoord(1, -1));

        Assert.True(storage.HasWorld(directory.Path));
        Assert.True(storage.HasChunkData(directory.Path, new ChunkCoord(1, -1)));
        Assert.Equal(metadata.WorldId, loadedMetadata.WorldId);
        Assert.Equal(metadata.Name, loadedMetadata.Name);
        Assert.NotNull(loadedChunk);
        Assert.Equal((ushort)9, loadedChunk.GetCell(2, 3).ForegroundTileId);
        Assert.Equal((ushort)4, loadedChunk.GetCell(2, 3).Variant);
    }

    [Fact]
    public void TryLoadChunk_WhenChunkDataDoesNotExist_ReturnsNull()
    {
        using var directory = new TestDirectoryScope();
        var storage = new WorldStorage();

        Assert.Null(storage.TryLoadChunk(directory.Path, new ChunkCoord(0, 0)));
        Assert.False(storage.HasChunkData(directory.Path, new ChunkCoord(0, 0)));
    }

    [Fact]
    public void SaveAndLoad_PlayerAndRuntimeEntities_UseSeparateFiles()
    {
        using var directory = new TestDirectoryScope();
        var storage = new WorldStorage();
        var players = new[]
        {
            new Entity
            {
                EntityId = 1,
                Type = EntityType.Player,
                Position = new Float2(12.5f, 8.25f),
                Velocity = new Float2(1.5f, -0.5f),
                LocalBounds = new AabbF(0.05f, 0.05f, 0.9f, 1.9f),
                StateFlags = EntityStateFlags.Grounded
            }
        };
        var runtimeEntities = new[]
        {
            new Entity
            {
                EntityId = 2,
                Type = EntityType.Drop,
                Position = new Float2(4.5f, 9.5f),
                Velocity = new Float2(0.25f, 0.75f),
                LocalBounds = new AabbF(0.15f, 0.15f, 0.7f, 0.7f),
                ItemDefId = 1001,
                Amount = 3
            }
        };

        storage.SavePlayers(directory.Path, players);
        storage.SaveRuntimeEntities(directory.Path, runtimeEntities);

        var loadedPlayers = storage.LoadPlayers(directory.Path);
        var loadedRuntimeEntities = storage.LoadRuntimeEntities(directory.Path);

        Assert.True(File.Exists(Path.Combine(directory.Path, "playerdata", "players.json")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "entities", "entities.bin")));
        Assert.Single(loadedPlayers);
        Assert.Single(loadedRuntimeEntities);
        Assert.Equal(players[0].Position, loadedPlayers[0].Position);
        Assert.Equal(runtimeEntities[0].Position, loadedRuntimeEntities[0].Position);
        Assert.Equal(runtimeEntities[0].ItemDefId, loadedRuntimeEntities[0].ItemDefId);
        Assert.Equal(runtimeEntities[0].Amount, loadedRuntimeEntities[0].Amount);
    }

    [Fact]
    public void LoadRuntimeEntities_WhenLegacyJsonExists_UsesCompatibilityFallback()
    {
        using var directory = new TestDirectoryScope();
        var storage = new WorldStorage();
        var legacySerializer = new EntityPersistenceSerializer();
        var entitiesDirectoryPath = Path.Combine(directory.Path, "entities");
        Directory.CreateDirectory(entitiesDirectoryPath);
        File.WriteAllText(
            Path.Combine(entitiesDirectoryPath, "entities.json"),
            legacySerializer.Serialize(
            [
                new Entity
                {
                    EntityId = 3,
                    Type = EntityType.Drop,
                    Position = new Float2(2.5f, 7.75f),
                    Velocity = new Float2(0.5f, 0.25f),
                    LocalBounds = new AabbF(0.15f, 0.15f, 0.7f, 0.7f),
                    ItemDefId = 1001,
                    Amount = 2
                }
            ]));

        var loadedRuntimeEntities = storage.LoadRuntimeEntities(directory.Path);

        var restoredEntity = Assert.Single(loadedRuntimeEntities);
        Assert.Equal(new Float2(2.5f, 7.75f), restoredEntity.Position);
        Assert.Equal(1001, restoredEntity.ItemDefId);
        Assert.Equal(2, restoredEntity.Amount);
    }
}
