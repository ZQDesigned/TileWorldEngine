using System;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime.Entities;

public sealed class EntityLiquidInteractionTests
{
    [Fact]
    public void Update_PlayerSubmersionState_IsDerivedFromLiquidCells()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));
        var playerId = runtime.SpawnPlayer(new Float2(5.5f, 5f));
        FillPlayerLiquidFootprint(runtime, (byte)LiquidKind.Water);

        runtime.Update(CreateFrameTime(1d / 60d));

        Assert.True(runtime.TryGetEntity(playerId, out var player));
        Assert.True(player.IsInLiquid);
        Assert.Equal(LiquidKind.Water, player.CurrentLiquidType);
        Assert.True(player.Submersion > 0f, $"Expected positive submersion but got {player.Submersion}.");
    }

    [Fact]
    public void Update_WhenPlayerLeavesLiquid_RuntimeStateResetsToAir()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));
        var playerId = runtime.SpawnPlayer(new Float2(5.5f, 5f));
        FillPlayerLiquidFootprint(runtime, (byte)LiquidKind.Water);

        runtime.Update(CreateFrameTime(1d / 60d));
        runtime.TryGetEntity(playerId, out var player);
        player.Position = new Float2(5.5f, 2f);
        player.Velocity = Float2.Zero;

        runtime.Update(CreateFrameTime(2d / 60d));

        Assert.True(runtime.TryGetEntity(playerId, out player));
        Assert.False(player.IsInLiquid);
        Assert.Equal(LiquidKind.None, player.CurrentLiquidType);
        Assert.Equal(0f, player.Submersion);
    }

    [Fact]
    public void Update_PlayerVerticalMotionDiffersAcrossLiquidKinds()
    {
        var waterVelocity = SimulateVerticalVelocityInLiquid((byte)LiquidKind.Water);
        var honeyVelocity = SimulateVerticalVelocityInLiquid((byte)LiquidKind.Honey);
        var lavaVelocity = SimulateVerticalVelocityInLiquid((byte)LiquidKind.Lava);

        Assert.True(waterVelocity < honeyVelocity);
        Assert.True(honeyVelocity < lavaVelocity);
    }

    [Fact]
    public void Update_PlayerAirMotion_IsUnaffectedWhenNoLiquidPresent()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));
        var playerId = runtime.SpawnPlayer(new Float2(2f, 2f));

        runtime.Update(CreateFrameTime(1d / 60d));

        Assert.True(runtime.TryGetEntity(playerId, out var player));
        Assert.False(player.IsInLiquid);
        Assert.Equal(LiquidKind.None, player.CurrentLiquidType);
        Assert.True(player.Velocity.Y > 0.4f);
    }

    private static float SimulateVerticalVelocityInLiquid(byte liquidType)
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));
        var playerId = runtime.SpawnPlayer(new Float2(5.5f, 5f));
        FillPlayerLiquidFootprint(runtime, liquidType);

        runtime.Update(CreateFrameTime(1d / 60d));
        runtime.TryGetEntity(playerId, out var player);
        return player.Velocity.Y;
    }

    private static void FillPlayerLiquidFootprint(WorldRuntime runtime, byte liquidType)
    {
        runtime.SetLiquid(new WorldTileCoord(5, 5), liquidType, 255);
        runtime.SetLiquid(new WorldTileCoord(6, 5), liquidType, 255);
        runtime.SetLiquid(new WorldTileCoord(5, 6), liquidType, 255);
        runtime.SetLiquid(new WorldTileCoord(6, 6), liquidType, 255);
    }

    private static FrameTime CreateFrameTime(double totalSeconds)
    {
        var totalTime = TimeSpan.FromSeconds(totalSeconds);
        return new FrameTime(totalTime, TimeSpan.FromSeconds(1d / 60d), false);
    }

    private static WorldRuntime CreateRuntime()
    {
        var contentRegistry = new ContentRegistry();
        contentRegistry.RegisterTile(new TileDef
        {
            Id = 1,
            Name = "Stone",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1
        });

        return new WorldRuntime(
            new WorldData(new WorldMetadata()),
            contentRegistry,
            new WorldRuntimeOptions
            {
                EnableAutoSave = false,
                SaveOnShutdown = false,
                EnableLiquidSimulation = false
            });
    }
}
