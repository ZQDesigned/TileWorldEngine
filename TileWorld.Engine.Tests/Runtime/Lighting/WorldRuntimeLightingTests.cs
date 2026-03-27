using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.Storage;
using TileWorld.Engine.Tests.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime.Lighting;

public sealed class WorldRuntimeLightingTests
{
    [Fact]
    public void PlaceTile_IncludesLightDirtyInResult()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();

        var result = runtime.PlaceTile(
            new WorldTileCoord(0, 0),
            1,
            new TilePlacementContext { Source = PlacementSource.DebugTool });

        Assert.True(result.Success);
        Assert.True((result.DirtyFlagsApplied & ChunkDirtyFlags.LightDirty) != ChunkDirtyFlags.None);
    }

    [Fact]
    public void GetLightLevel_OpenSkyIsBrighterThanEnclosedUnderground()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateGeneratedRuntime(directory.Path, new WorldMetadata
        {
            WorldId = "lighting-flat-world",
            Name = "Lighting Flat World",
            GeneratorId = "flat_debug"
        });
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));
        var skyLight = runtime.GetLightLevel(new WorldTileCoord(0, 1));
        var undergroundLight = runtime.GetLightLevel(new WorldTileCoord(0, 6));

        Assert.True(skyLight > undergroundLight);
    }

    [Fact]
    public void SkyLight_GraduallyDecaysBelowSurfaceUntilDarkness()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateGeneratedRuntime(directory.Path, new WorldMetadata
        {
            WorldId = "lighting-gradient-world",
            Name = "Lighting Gradient World",
            GeneratorId = "flat_debug"
        });
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));

        var surfaceLight = runtime.GetLightLevel(new WorldTileCoord(0, 2));
        var shallowUndergroundLight = runtime.GetLightLevel(new WorldTileCoord(0, 3));
        var midUndergroundLight = runtime.GetLightLevel(new WorldTileCoord(0, 5));
        var deepUndergroundLight = runtime.GetLightLevel(new WorldTileCoord(0, 10));

        Assert.True(surfaceLight > shallowUndergroundLight);
        Assert.True(shallowUndergroundLight > midUndergroundLight);
        Assert.Equal(0, deepUndergroundLight);
    }

    [Fact]
    public void DeepUndergroundChunk_DoesNotTreatWindowTopAsOpenSky()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateGeneratedRuntime(directory.Path, new WorldMetadata
        {
            WorldId = "lighting-underground-world",
            Name = "Lighting Underground World",
            GeneratorId = "flat_debug"
        });
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 1));

        var undergroundChunkTopLight = runtime.GetLightLevel(new WorldTileCoord(0, 32));

        Assert.Equal(0, undergroundChunkTopLight);
    }

    [Fact]
    public void EmissiveObject_PropagatesLightAcrossChunkBoundary()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));
        runtime.EnsureChunkLoaded(new ChunkCoord(1, 0));

        for (var x = 30; x <= 33; x++)
        {
            runtime.SetForegroundTile(new WorldTileCoord(x, 4), 1);
            runtime.SetForegroundTile(new WorldTileCoord(x, 7), 1);
        }

        runtime.SetForegroundTile(new WorldTileCoord(30, 5), 1);
        runtime.SetForegroundTile(new WorldTileCoord(30, 6), 1);
        runtime.SetForegroundTile(new WorldTileCoord(33, 5), 1);
        runtime.SetForegroundTile(new WorldTileCoord(33, 6), 1);

        var placement = runtime.PlaceObject(
            new WorldTileCoord(31, 6),
            100,
            new ObjectPlacementContext
            {
                Source = PlacementSource.DebugTool
            });

        var adjacentChunkLight = runtime.GetLightLevel(new WorldTileCoord(32, 6));

        Assert.True(placement.Success);
        Assert.True(adjacentChunkLight > 0);
    }

    [Fact]
    public void Player_ProvidesBaseLightInDarkness()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));
        var playerId = runtime.SpawnPlayer(new Float2(4.5f, 4.5f));

        runtime.Update(new TileWorld.Engine.Hosting.FrameTime(TimeSpan.FromSeconds(1d / 60d), TimeSpan.FromSeconds(1d / 60d), false));

        Assert.True(runtime.TryGetEntity(playerId, out var player));
        var playerCenter = GetEntityCenterTile(player);
        Assert.True(runtime.GetLightLevel(playerCenter) > 0);
    }

    [Fact]
    public void HeldLight_IncreasesPlayerLightRadius()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateGeneratedRuntime(directory.Path, new WorldMetadata
        {
            WorldId = "lighting-held-light-world",
            Name = "Lighting Held Light World",
            GeneratorId = "flat_debug"
        });
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));
        var playerId = runtime.SpawnPlayer(new Float2(4.5f, 13.5f));
        var probeCoord = new WorldTileCoord(9, 13);

        for (var x = 1; x <= 11; x++)
        {
            for (var y = 11; y <= 16; y++)
            {
                runtime.RemoveForegroundTile(new WorldTileCoord(x, y));
            }
        }

        for (var y = 11; y <= 16; y++)
        {
            runtime.SetForegroundTile(new WorldTileCoord(1, y), 1);
            runtime.SetForegroundTile(new WorldTileCoord(11, y), 1);
        }

        for (var x = 1; x <= 11; x++)
        {
            runtime.SetForegroundTile(new WorldTileCoord(x, 11), 1);
            runtime.SetForegroundTile(new WorldTileCoord(x, 16), 1);
        }

        runtime.Update(new TileWorld.Engine.Hosting.FrameTime(TimeSpan.FromSeconds(1d / 60d), TimeSpan.FromSeconds(1d / 60d), false));
        var baseLight = runtime.GetLightLevel(probeCoord);

        runtime.SetPlayerHeldLightLevel(playerId, 14);
        runtime.Update(new TileWorld.Engine.Hosting.FrameTime(TimeSpan.FromSeconds(2d / 60d), TimeSpan.FromSeconds(1d / 60d), false));
        var boostedLight = runtime.GetLightLevel(probeCoord);

        Assert.True(
            boostedLight > baseLight,
            $"Expected held light to increase brightness at {probeCoord}, but base={baseLight}, boosted={boostedLight}.");
    }

    [Fact]
    public void BlockingTile_AttenuatesLightInsteadOfStoppingItCompletely()
    {
        using var directory = new TestDirectoryScope();
        var runtime = CreateGeneratedRuntime(directory.Path, new WorldMetadata
        {
            WorldId = "lighting-blocking-world",
            Name = "Lighting Blocking World",
            GeneratorId = "flat_debug"
        });
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));

        var surfaceTileLight = runtime.GetLightLevel(new WorldTileCoord(0, 2));
        var oneLayerBelowLight = runtime.GetLightLevel(new WorldTileCoord(0, 3));
        var twoLayersBelowLight = runtime.GetLightLevel(new WorldTileCoord(0, 4));

        Assert.True(oneLayerBelowLight > 0);
        Assert.True(surfaceTileLight > oneLayerBelowLight);
        Assert.True(oneLayerBelowLight > twoLayersBelowLight);
    }

    private static WorldRuntime CreateRuntime(WorldMetadata? metadata = null)
    {
        return new WorldRuntime(new WorldData(metadata ?? new WorldMetadata()), CreateRegistry());
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
        registry.RegisterObject(new ObjectDef
        {
            Id = 100,
            Name = "Lamp",
            SizeInTiles = Int2.One,
            RequiresSupport = false,
            EmissiveLight = 14
        });
        registry.RegisterWall(new TileWorld.Engine.Content.Walls.WallDef
        {
            Id = 1,
            Name = "Stone Wall"
        });
        registry.RegisterWall(new TileWorld.Engine.Content.Walls.WallDef
        {
            Id = 2,
            Name = "Dirt Wall"
        });

        return registry;
    }

    private static WorldRuntime CreateGeneratedRuntime(string worldPath, WorldMetadata metadata)
    {
        return new WorldRuntime(
            new WorldData(metadata),
            CreateRegistry(),
            new WorldRuntimeOptions
            {
                WorldPath = worldPath,
                WorldStorage = new WorldStorage(),
                SaveOnShutdown = false,
                EnableAutoSave = false
            });
    }

    private static WorldTileCoord GetEntityCenterTile(Entity entity)
    {
        var bounds = entity.WorldBounds;
        return new WorldTileCoord(
            (int)MathF.Floor(bounds.Left + (bounds.Width / 2f)),
            (int)MathF.Floor(bounds.Top + (bounds.Height / 2f)));
    }
}
