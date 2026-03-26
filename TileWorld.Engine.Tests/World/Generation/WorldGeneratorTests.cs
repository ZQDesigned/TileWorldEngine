using System.Linq;
using TileWorld.Engine.Content.Biomes;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Generation;
using TileWorld.Engine.World;

namespace TileWorld.Engine.Tests.World.Generation;

public sealed class WorldGeneratorTests
{
    [Fact]
    public void OverworldGenerator_WithSameSeed_GeneratesIdenticalChunkData()
    {
        var generator = new OverworldWorldGenerator();
        var context = CreateGenerationContext(seed: 1337);

        var firstChunk = generator.GenerateChunk(context, new ChunkCoord(2, 1)).Chunk;
        var secondChunk = generator.GenerateChunk(context, new ChunkCoord(2, 1)).Chunk;

        AssertChunksEqual(firstChunk, secondChunk);
    }

    [Fact]
    public void OverworldGenerator_WithDifferentSeeds_GeneratesDifferentChunkData()
    {
        var generator = new OverworldWorldGenerator();
        var firstChunk = generator.GenerateChunk(CreateGenerationContext(seed: 1001), new ChunkCoord(4, 1)).Chunk;
        var secondChunk = generator.GenerateChunk(CreateGenerationContext(seed: 2002), new ChunkCoord(4, 1)).Chunk;

        Assert.True(ChunksDiffer(firstChunk, secondChunk));
    }

    [Fact]
    public void OverworldGenerator_CreatesWalkableSpawnRegion()
    {
        var generator = new OverworldWorldGenerator();
        var context = CreateGenerationContext(seed: 98765);
        var spawnTile = new WorldTileCoord(context.Metadata.SpawnTile.X, context.Metadata.SpawnTile.Y);
        var spawnChunk = generator.GenerateChunk(context, WorldCoordinateConverter.ToChunkCoord(spawnTile)).Chunk;
        var spawnLocal = WorldCoordinateConverter.ToLocalCoord(spawnTile);
        var spawnGroundLocal = WorldCoordinateConverter.ToLocalCoord(new WorldTileCoord(spawnTile.X, spawnTile.Y + 2));

        Assert.Equal((ushort)0, spawnChunk.GetCell(spawnLocal.X, spawnLocal.Y).ForegroundTileId);
        Assert.NotEqual((ushort)0, spawnChunk.GetCell(spawnGroundLocal.X, spawnGroundLocal.Y).ForegroundTileId);
    }

    [Fact]
    public void OverworldGenerator_CreatesSmoothedSurfaceProfileAroundSpawn()
    {
        var generator = new OverworldWorldGenerator();
        var context = CreateGenerationContext(seed: 24680);
        var surfaceHeights = Enumerable.Range(-96, 193)
            .Select(offset => FindSurfaceY(generator, context, context.Metadata.SpawnTile.X + offset))
            .ToArray();

        var distinctHeightCount = surfaceHeights.Distinct().Count();
        var maxAdjacentStep = surfaceHeights
            .Zip(surfaceHeights.Skip(1), static (left, right) => Math.Abs(right - left))
            .Max();

        Assert.True(distinctHeightCount >= 4);
        Assert.True(maxAdjacentStep <= 2, $"Expected a smoothed surface profile, but adjacent step was {maxAdjacentStep}.");
    }

    [Fact]
    public void OverworldGenerator_DoesNotExposeOreInNearSurfaceBand()
    {
        var generator = new OverworldWorldGenerator();
        var context = CreateGenerationContext(seed: 13579);

        foreach (var offset in Enumerable.Range(-96, 193))
        {
            var worldX = context.Metadata.SpawnTile.X + offset;
            var surfaceY = FindSurfaceY(generator, context, worldX);

            for (var worldY = surfaceY; worldY <= surfaceY + 8; worldY++)
            {
                Assert.NotEqual((ushort)3, GetCell(generator, context, worldX, worldY).ForegroundTileId);
            }
        }
    }

    [Fact]
    public void OverworldGenerator_ProtectsSpawnFromImmediateCaves()
    {
        var generator = new OverworldWorldGenerator();
        var context = CreateGenerationContext(seed: 112233);

        foreach (var offset in Enumerable.Range(-20, 41))
        {
            var worldX = context.Metadata.SpawnTile.X + offset;
            var surfaceY = FindSurfaceY(generator, context, worldX);

            for (var worldY = surfaceY + 1; worldY <= surfaceY + 8; worldY++)
            {
                Assert.NotEqual((ushort)0, GetCell(generator, context, worldX, worldY).ForegroundTileId);
            }
        }
    }

    [Fact]
    public void Generators_RespectOptionalVerticalBounds()
    {
        var generator = new FlatDebugWorldGenerator();
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

    private static int FindSurfaceY(IWorldGenerator generator, WorldGenerationContext context, int worldX)
    {
        for (var worldY = context.Metadata.SpawnTile.Y - 8; worldY <= context.Metadata.SpawnTile.Y + 48; worldY++)
        {
            if (GetCell(generator, context, worldX, worldY).ForegroundTileId != 0)
            {
                return worldY;
            }
        }

        throw new Xunit.Sdk.XunitException($"No surface tile was generated for world column {worldX}.");
    }

    private static TileCell GetCell(IWorldGenerator generator, WorldGenerationContext context, int worldX, int worldY)
    {
        var coord = new WorldTileCoord(worldX, worldY);
        var chunkCoord = WorldCoordinateConverter.ToChunkCoord(coord);
        var chunk = generator.GenerateChunk(context, chunkCoord).Chunk;
        var localCoord = WorldCoordinateConverter.ToLocalCoord(coord);
        return chunk.GetCell(localCoord.X, localCoord.Y);
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
