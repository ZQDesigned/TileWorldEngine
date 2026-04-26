using System.Linq;
using TileWorld.Engine.Content.Biomes;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Generation;

namespace TileWorld.Engine.Tests.World.Generation;

public sealed class WorldGeneratorTests
{
    [Fact]
    public void Registry_ResolvesRegisteredGeneratorAndAlias()
    {
        var registry = new WorldGeneratorRegistry();
        var generator = new FlatTestWorldGenerator();
        registry.Register(generator);
        registry.RegisterAlias("flat_test_v1", generator);

        Assert.True(registry.TryResolve(FlatTestWorldGenerator.GeneratorIdValue, out var directResolution));
        Assert.True(registry.TryResolve("flat_test_v1", out var aliasResolution));
        Assert.Same(generator, directResolution);
        Assert.Same(generator, aliasResolution);
    }

    [Fact]
    public void DeterministicGenerator_WithSameSeed_GeneratesIdenticalChunkData()
    {
        var generator = new DeterministicTerrainTestWorldGenerator();
        var context = CreateGenerationContext(seed: 1337);

        var firstChunk = generator.GenerateChunk(context, new ChunkCoord(2, 1)).Chunk;
        var secondChunk = generator.GenerateChunk(context, new ChunkCoord(2, 1)).Chunk;

        AssertChunksEqual(firstChunk, secondChunk);
    }

    [Fact]
    public void DeterministicGenerator_WithDifferentSeeds_GeneratesDifferentChunkData()
    {
        var generator = new DeterministicTerrainTestWorldGenerator();
        var firstContext = CreateGenerationContext(seed: 1001);
        var secondContext = CreateGenerationContext(seed: 2002);
        var firstProfile = Enumerable.Range(-32, 65)
            .Select(offset => generator.GetSurfaceHeight(firstContext, firstContext.Metadata.SpawnTile.X + offset))
            .ToArray();
        var secondProfile = Enumerable.Range(-32, 65)
            .Select(offset => generator.GetSurfaceHeight(secondContext, secondContext.Metadata.SpawnTile.X + offset))
            .ToArray();

        Assert.False(firstProfile.SequenceEqual(secondProfile));
    }

    [Fact]
    public void FlatGenerator_CreatesWalkableSpawnRegion()
    {
        var generator = new FlatTestWorldGenerator();
        var context = CreateGenerationContext(seed: 98765);
        var spawnTile = new WorldTileCoord(context.Metadata.SpawnTile.X, context.Metadata.SpawnTile.Y);
        var spawnChunk = generator.GenerateChunk(context, WorldCoordinateConverter.ToChunkCoord(spawnTile)).Chunk;
        var spawnLocal = WorldCoordinateConverter.ToLocalCoord(spawnTile);
        var spawnGroundLocal = WorldCoordinateConverter.ToLocalCoord(new WorldTileCoord(spawnTile.X, spawnTile.Y + 2));

        Assert.Equal((ushort)0, spawnChunk.GetCell(spawnLocal.X, spawnLocal.Y).ForegroundTileId);
        Assert.NotEqual((ushort)0, spawnChunk.GetCell(spawnGroundLocal.X, spawnGroundLocal.Y).ForegroundTileId);
    }

    [Fact]
    public void FlatGenerator_RespectsOptionalVerticalBounds()
    {
        var generator = new FlatTestWorldGenerator();
        var context = new WorldGenerationContext
        {
            Metadata = new WorldMetadata
            {
                WorldId = "bounded-world",
                Name = "Bounded World",
                Seed = 1,
                SpawnTile = new Int2(4, 4),
                MinTileY = 0,
                MaxTileY = 15
            },
            ContentRegistry = CreateRegistry()
        };

        var upperChunk = generator.GenerateChunk(context, new ChunkCoord(0, -1)).Chunk;
        var lowerChunk = generator.GenerateChunk(context, new ChunkCoord(0, 0)).Chunk;

        Assert.All(Enumerable.Range(0, ChunkDimensions.CellCount), index =>
        {
            var localX = index % ChunkDimensions.Width;
            var localY = index / ChunkDimensions.Width;
            Assert.Equal(TileCell.Empty, upperChunk.GetCell(localX, localY));
        });
        Assert.NotEqual(TileCell.Empty, lowerChunk.GetCell(0, 6));
        Assert.Equal(TileCell.Empty, lowerChunk.GetCell(0, 16));
    }

    private static WorldGenerationContext CreateGenerationContext(int seed)
    {
        return new WorldGenerationContext
        {
            Metadata = new WorldMetadata
            {
                WorldId = $"world-{seed}",
                Name = "Generator Test",
                Seed = seed,
                SpawnTile = new Int2(4, 18)
            },
            ContentRegistry = CreateRegistry()
        };
    }

    private static ContentRegistry CreateRegistry()
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

        return registry;
    }

    private static void AssertChunksEqual(Chunk expected, Chunk actual)
    {
        for (var localY = 0; localY < ChunkDimensions.Height; localY++)
        {
            for (var localX = 0; localX < ChunkDimensions.Width; localX++)
            {
                Assert.Equal(expected.GetCell(localX, localY), actual.GetCell(localX, localY));
            }
        }
    }

    private static bool ChunksDiffer(Chunk first, Chunk second)
    {
        for (var localY = 0; localY < ChunkDimensions.Height; localY++)
        {
            for (var localX = 0; localX < ChunkDimensions.Width; localX++)
            {
                if (first.GetCell(localX, localY) != second.GetCell(localX, localY))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
