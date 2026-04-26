using System;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime.Liquids;

public sealed class WorldRuntimeLiquidTests
{
    [Fact]
    public void SetLiquid_ThenTryGetLiquid_ReturnsStoredValues()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));

        var updated = runtime.SetLiquid(new WorldTileCoord(5, 5), (byte)LiquidKind.Water, 180);
        var resolved = runtime.TryGetLiquid(new WorldTileCoord(5, 5), out var liquidType, out var liquidAmount);

        Assert.True(updated);
        Assert.True(resolved);
        Assert.Equal((byte)LiquidKind.Water, liquidType);
        Assert.Equal((byte)180, liquidAmount);
    }

    [Fact]
    public void GetLiquidState_ReturnsNone_WhenCellHasNoLiquid()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();

        var state = runtime.GetLiquidState(new WorldTileCoord(2, 2));

        Assert.False(state.HasLiquid);
        Assert.Equal(LiquidKind.None, state.Kind);
        Assert.Equal((byte)0, state.Amount);
    }

    [Fact]
    public void Update_LiquidFallsDownward_WhenSpaceBelowIsOpen()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));

        runtime.SetLiquid(new WorldTileCoord(8, 6), (byte)LiquidKind.Water, 200);
        runtime.Update(new FrameTime(TimeSpan.FromSeconds(1d / 60d), TimeSpan.FromSeconds(1d / 60d), false));

        runtime.TryGetLiquid(new WorldTileCoord(8, 6), out _, out var sourceAmount);
        runtime.TryGetLiquid(new WorldTileCoord(8, 7), out _, out var belowAmount);

        Assert.True(belowAmount > 0);
        Assert.True(sourceAmount < 200);
    }

    [Fact]
    public void Update_LiquidFlowsSideways_WhenBlockedBelow()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));

        runtime.SetForegroundTile(new WorldTileCoord(10, 7), 1);
        runtime.SetLiquid(new WorldTileCoord(10, 6), (byte)LiquidKind.Water, 220);
        runtime.Update(new FrameTime(TimeSpan.FromSeconds(1d / 60d), TimeSpan.FromSeconds(1d / 60d), false));

        runtime.TryGetLiquid(new WorldTileCoord(9, 6), out _, out var leftAmount);
        runtime.TryGetLiquid(new WorldTileCoord(11, 6), out _, out var rightAmount);
        var sideAmount = Math.Max(leftAmount, rightAmount);

        Assert.True(sideAmount > 0);
        Assert.False(runtime.TryGetLiquid(new WorldTileCoord(10, 7), out _, out _));
    }

    [Fact]
    public void Update_LiquidPrioritizesDownwardFlow_BeforeSidewaysSpread()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));

        runtime.SetLiquid(new WorldTileCoord(12, 6), (byte)LiquidKind.Water, 255);
        runtime.Update(new FrameTime(TimeSpan.FromSeconds(1d / 60d), TimeSpan.FromSeconds(1d / 60d), false));

        runtime.TryGetLiquid(new WorldTileCoord(12, 7), out _, out var belowAmount);
        var hasLeft = runtime.TryGetLiquid(new WorldTileCoord(11, 6), out _, out var leftAmount);
        var hasRight = runtime.TryGetLiquid(new WorldTileCoord(13, 6), out _, out var rightAmount);

        Assert.Equal((byte)255, belowAmount);
        Assert.False(hasLeft && leftAmount > 0);
        Assert.False(hasRight && rightAmount > 0);
    }

    [Fact]
    public void Update_LateralSpread_DoesNotProduceTinyTraceAmounts()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));

        runtime.SetForegroundTile(new WorldTileCoord(14, 7), 1);
        runtime.SetLiquid(new WorldTileCoord(14, 6), (byte)LiquidKind.Water, 255);

        for (var i = 0; i < 12; i++)
        {
            runtime.Update(new FrameTime(TimeSpan.FromSeconds((i + 1) / 60d), TimeSpan.FromSeconds(1d / 60d), false));
        }

        var chunk = runtime.WorldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        for (var y = 0; y < ChunkDimensions.Height; y++)
        {
            for (var x = 0; x < ChunkDimensions.Width; x++)
            {
                var cell = chunk.GetCell(x, y);
                if (cell.LiquidAmount == 0)
                {
                    continue;
                }

                Assert.True(cell.LiquidAmount >= 8, $"Unexpected tiny liquid amount {cell.LiquidAmount} at ({x},{y}).");
            }
        }
    }

    [Fact]
    public void SetForegroundTile_SolidTileClearsExistingLiquid()
    {
        var runtime = CreateRuntime();
        runtime.Initialize();
        runtime.EnsureChunkLoaded(new ChunkCoord(0, 0));

        runtime.SetLiquid(new WorldTileCoord(4, 4), (byte)LiquidKind.Water, 160);
        runtime.SetForegroundTile(new WorldTileCoord(4, 4), 1);

        Assert.False(runtime.TryGetLiquid(new WorldTileCoord(4, 4), out _, out _));
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
                EnableLiquidSimulation = true
            });
    }
}
