using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;

namespace TileWorld.Engine.Tests.Storage;

public sealed class WorldCatalogTests
{
    [Fact]
    public void EnumerateWorlds_SortsNewestFirstAndSkipsInvalidDirectories()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var storage = new WorldStorage();
            storage.SaveMetadata(Path.Combine(rootPath, "older-world"), new WorldMetadata
            {
                WorldId = "older-world",
                Name = "Older World",
                SpawnTile = new Int2(1, 2)
            });
            storage.SaveMetadata(Path.Combine(rootPath, "newer-world"), new WorldMetadata
            {
                WorldId = "newer-world",
                Name = "Newer World",
                SpawnTile = new Int2(3, 4)
            });

            Directory.CreateDirectory(Path.Combine(rootPath, "older-world", "chunks"));
            Directory.CreateDirectory(Path.Combine(rootPath, "newer-world", "chunks"));
            var olderChunkPath = Path.Combine(rootPath, "older-world", "chunks", "0_0.chk");
            var newerChunkPath = Path.Combine(rootPath, "newer-world", "chunks", "0_0.chk");
            File.WriteAllBytes(olderChunkPath, [1, 2, 3]);
            File.WriteAllBytes(newerChunkPath, [4, 5, 6]);

            File.SetLastWriteTimeUtc(olderChunkPath, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(newerChunkPath, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

            Directory.CreateDirectory(Path.Combine(rootPath, "broken-world"));
            File.WriteAllText(Path.Combine(rootPath, "broken-world", "world.json"), "{ not valid json");

            var catalog = new WorldCatalog(rootPath, storage);
            var worlds = catalog.EnumerateWorlds();

            Assert.Equal(2, worlds.Count);
            Assert.Equal("newer-world", worlds[0].DirectoryName);
            Assert.Equal("older-world", worlds[1].DirectoryName);
            Assert.True(worlds[0].HasChunkData);
            Assert.True(worlds[1].HasChunkData);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void CreateWorld_CreatesUniqueDirectoriesAndMinimalMetadata()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var storage = new WorldStorage();
            var catalog = new WorldCatalog(rootPath, storage);

            var firstWorld = catalog.CreateWorld(new WorldCreationOptions
            {
                Name = "Sandbox World",
                GeneratorId = "sandbox_overworld",
                SpawnTile = new Int2(4, 18),
                MinTileY = -64,
                MaxTileY = 255
            });
            var secondWorld = catalog.CreateWorld(new WorldCreationOptions
            {
                Name = "Sandbox World",
                GeneratorId = "sandbox_overworld",
                SpawnTile = new Int2(8, 20)
            });

            Assert.NotEqual(firstWorld.WorldPath, secondWorld.WorldPath);
            Assert.NotEqual(firstWorld.DirectoryName, secondWorld.DirectoryName);
            Assert.Equal("Sandbox World", storage.LoadMetadata(firstWorld.WorldPath).Name);
            Assert.Equal("sandbox_overworld", storage.LoadMetadata(firstWorld.WorldPath).GeneratorId);
            Assert.Equal(-64, storage.LoadMetadata(firstWorld.WorldPath).MinTileY);
            Assert.Equal(255, storage.LoadMetadata(firstWorld.WorldPath).MaxTileY);
            Assert.Equal(new Int2(8, 20), storage.LoadMetadata(secondWorld.WorldPath).SpawnTile);
            Assert.False(firstWorld.HasChunkData);
            Assert.False(secondWorld.HasChunkData);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "TileWorldEngine.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
