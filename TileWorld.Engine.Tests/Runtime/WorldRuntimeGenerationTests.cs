using TileWorld.Engine.Content.Biomes;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Chunks;
using TileWorld.Engine.Storage;
using TileWorld.Engine.Tests.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime;

public sealed class WorldRuntimeGenerationTests
{
    [Fact]
    public void LoadChunk_ForFreshPersistedWorld_UsesGeneratedSourceThenDiskAfterSave()
    {
        using var directory = new TestDirectoryScope();
        var initialRuntime = CreateRuntime(directory.Path, new WorldMetadata
        {
            WorldId = "generated-world",
            Name = "Generated World",
            Seed = 12345,
            SpawnTile = new Int2(4, 18)
        });

        initialRuntime.Initialize();
        var generatedChunk = initialRuntime.LoadChunk(new ChunkCoord(0, 0));

        Assert.Equal(ChunkLoadSource.Generated, generatedChunk.Source);
        Assert.True((generatedChunk.Chunk.DirtyFlags & ChunkDirtyFlags.SaveDirty) != ChunkDirtyFlags.None);

        initialRuntime.SaveWorld();
        initialRuntime.Shutdown();

        var restoredRuntime = CreateRuntime(directory.Path, new WorldStorage().LoadMetadata(directory.Path));
        restoredRuntime.Initialize();
        var diskChunk = restoredRuntime.LoadChunk(new ChunkCoord(0, 0));

        Assert.Equal(ChunkLoadSource.Disk, diskChunk.Source);
    }

    [Fact]
    public void GetBiomeId_AndBiomeDefinition_AreStableAcrossChunkLoads()
    {
        var runtime = CreateRuntime(string.Empty, new WorldMetadata
        {
            WorldId = "biome-world",
            Name = "Biome World",
            Seed = 67890,
            SpawnTile = new Int2(4, 18)
        });

        runtime.Initialize();
        var leftCoord = new WorldTileCoord(31, 20);
        var rightCoord = new WorldTileCoord(32, 20);
        var leftBiomeBefore = runtime.GetBiomeId(leftCoord);
        var rightBiomeBefore = runtime.GetBiomeId(rightCoord);

        runtime.LoadChunk(WorldCoordinateConverter.ToChunkCoord(leftCoord));
        runtime.LoadChunk(WorldCoordinateConverter.ToChunkCoord(rightCoord));

        var leftBiomeAfter = runtime.GetBiomeId(leftCoord);
        var rightBiomeAfter = runtime.GetBiomeId(rightCoord);

        Assert.Equal(leftBiomeBefore, leftBiomeAfter);
        Assert.Equal(rightBiomeBefore, rightBiomeAfter);
        Assert.True(runtime.TryGetBiomeDef(leftCoord, out var leftBiomeDef));
        Assert.True(runtime.TryGetBiomeDef(rightCoord, out var rightBiomeDef));
        Assert.NotNull(leftBiomeDef);
        Assert.NotNull(rightBiomeDef);
    }

    private static WorldRuntime CreateRuntime(string worldPath, WorldMetadata metadata)
    {
        var registry = new ContentRegistry();
        registry.RegisterTile(new TileDef
        {
            Id = 1,
            Name = "Stone",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1
        });
        registry.RegisterTile(new TileDef
        {
            Id = 2,
            Name = "Dirt",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1
        });
        registry.RegisterTile(new TileDef
        {
            Id = 3,
            Name = "Copper Ore",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1
        });
        registry.RegisterWall(new WallDef
        {
            Id = 1,
            Name = "Stone Wall"
        });
        registry.RegisterWall(new WallDef
        {
            Id = 2,
            Name = "Dirt Wall"
        });
        registry.RegisterBiome(new BiomeDef
        {
            Id = 1,
            Name = "Plains",
            SurfaceTileId = 2,
            SubsurfaceTileId = 1,
            SurfaceWallId = 2,
            Priority = 5
        });
        registry.RegisterBiome(new BiomeDef
        {
            Id = 2,
            Name = "Rocky",
            SurfaceTileId = 1,
            SubsurfaceTileId = 1,
            SurfaceWallId = 1,
            Priority = 10
        });

        if (string.IsNullOrWhiteSpace(worldPath))
        {
            return new WorldRuntime(new WorldData(metadata), registry);
        }

        return new WorldRuntime(
            new WorldData(metadata),
            registry,
            new WorldRuntimeOptions
            {
                WorldPath = worldPath,
                WorldStorage = new WorldStorage(),
                SaveOnShutdown = true,
                EnableAutoSave = false
            });
    }
}
