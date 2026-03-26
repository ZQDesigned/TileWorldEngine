using System;
using System.IO;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Input;
using TileWorld.Engine.Render;
using TileWorld.Engine.Runtime;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Operations;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Testing.Desktop;

internal sealed class SmokeTestEngineApplication : IEngineApplication
{
    private const string DebugWhiteTextureKey = "debug/white";
    private const ushort StoneTileId = 1;
    private const ushort DirtTileId = 2;
    private const ushort BrickTileId = 3;
    private const double CameraSpeedPixelsPerSecond = 240d;
    private const double CameraSpeedBoostMultiplier = 3d;

    private DebugOverlayRenderer _debugOverlayRenderer = null!;
    private FrameInput _lastFrameInput = FrameInput.Empty;
    private bool _isInitialized;
    private bool _isDebugOverlayEnabled = true;
    private bool _isNewWorld;
    private Camera2D _camera = null!;
    private ContentRegistry _contentRegistry = null!;
    private WorldRenderSettings _renderSettings = null!;
    private ushort _selectedTileId = StoneTileId;
    private string _worldPath = string.Empty;
    private WorldRenderer _worldRenderer = null!;
    private WorldRuntime _worldRuntime = null!;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _contentRegistry = new ContentRegistry();
        _renderSettings = new WorldRenderSettings();
        _debugOverlayRenderer = new DebugOverlayRenderer(_renderSettings);
        _worldPath = Path.Combine(AppContext.BaseDirectory, "Worlds", "smoke-test-world");
        _contentRegistry.RegisterTile(new TileDef
        {
            Id = StoneTileId,
            Name = "Stone",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1,
            AutoTileGroupId = 1,
            Visual = new TileVisualDef(
                DebugWhiteTextureKey,
                new RectI(0, 0, 1, 1),
                new ColorRgba32(125, 125, 125),
                false)
        });
        _contentRegistry.RegisterTile(new TileDef
        {
            Id = DirtTileId,
            Name = "Dirt",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1,
            AutoTileGroupId = 2,
            Visual = new TileVisualDef(
                DebugWhiteTextureKey,
                new RectI(0, 0, 1, 1),
                new ColorRgba32(139, 103, 68),
                false)
        });
        _contentRegistry.RegisterTile(new TileDef
        {
            Id = BrickTileId,
            Name = "Brick",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1,
            AutoTileGroupId = 3,
            Visual = new TileVisualDef(
                DebugWhiteTextureKey,
                new RectI(0, 0, 1, 1),
                new ColorRgba32(180, 70, 60),
                false)
        });

        var worldStorage = new WorldStorage();
        _isNewWorld = !worldStorage.HasWorld(_worldPath);
        var metadata = _isNewWorld
            ? new WorldMetadata
            {
                WorldId = "smoke-test-world",
                Name = "Smoke Test World"
            }
            : worldStorage.LoadMetadata(_worldPath);

        _worldRuntime = new WorldRuntime(
            new WorldData(metadata),
            _contentRegistry,
            new WorldRuntimeOptions
            {
                WorldPath = _worldPath,
                WorldStorage = worldStorage,
                SaveOnShutdown = true,
                EnableAutoSave = true,
                AutoSaveInterval = TimeSpan.FromSeconds(30),
                AutoSaveIdleDelay = TimeSpan.FromSeconds(4),
                MinimumAutoSaveSpacing = TimeSpan.FromSeconds(8)
            });
        _camera = new Camera2D(new Int2(-160, 144), new Int2(800, 480));
        _worldRenderer = new WorldRenderer(
            _camera,
            new ChunkRenderCacheBuilder(_contentRegistry, _renderSettings),
            _renderSettings);
        _worldRuntime.Subscribe<TileChangedEvent>(evt =>
            EngineDiagnostics.Info($"TileChanged: Coord={evt.Coord}, Old={evt.OldTileId}, New={evt.NewTileId}"));
        _worldRuntime.Subscribe<TilePlacedEvent>(evt =>
            EngineDiagnostics.Info($"TilePlaced: Coord={evt.Coord}, Tile={evt.TileId}, Source={evt.Source}"));
        _worldRuntime.Subscribe<TileBrokenEvent>(evt =>
            EngineDiagnostics.Info($"TileBroken: Coord={evt.Coord}, Previous={evt.PreviousTileId}, Source={evt.Source}"));

        _worldRuntime.Initialize();
        if (_isNewWorld)
        {
            PopulateSmokeTerrain();
        }
        else
        {
            EnsureVisibleSmokeChunksLoaded();
        }

        LogInitializationSummary(metadata);

        _isInitialized = true;
    }

    public void Update(FrameTime frameTime, FrameInput input)
    {
        if (!_isInitialized)
        {
            return;
        }

        _lastFrameInput = input ?? FrameInput.Empty;
        _worldRuntime.Update(frameTime);
        UpdateSelectedTile(input);
        UpdateDebugOverlayToggle(input);
        UpdateCamera(frameTime, input);
        UpdateManualSave(frameTime, input);
        UpdateTileEdits(input);
    }

    public void Render(IRenderContext renderContext)
    {
        if (!_isInitialized)
        {
            return;
        }

        _camera.ViewportSizePixels = renderContext.ViewportSizePixels;
        renderContext.Clear(ColorRgba32.CornflowerBlue);
        _worldRenderer.RebuildDirtyCaches(_worldRuntime);
        _worldRenderer.Draw(_worldRuntime, renderContext);
        if (_isDebugOverlayEnabled)
        {
            _debugOverlayRenderer.Draw(
                _worldRuntime,
                _worldRenderer,
                _camera,
                _lastFrameInput,
                renderContext,
                _selectedTileId);
        }
    }

    public void Shutdown()
    {
        if (!_isInitialized)
        {
            return;
        }

        _worldRuntime.Shutdown();
        EngineDiagnostics.Info("Smoke application shutdown.");
        _isInitialized = false;
        _isDebugOverlayEnabled = true;
        _isNewWorld = false;
        _lastFrameInput = FrameInput.Empty;
        _selectedTileId = StoneTileId;
    }

    private void PopulateSmokeTerrain()
    {
        var placementContext = new TilePlacementContext
        {
            ActorEntityId = 0,
            Source = PlacementSource.WorldGeneration,
            SuppressEvents = true
        };

        for (var tileX = -32; tileX < 64; tileX++)
        {
            for (var tileY = 26; tileY < 40; tileY++)
            {
                _worldRuntime.PlaceTile(new WorldTileCoord(tileX, tileY), StoneTileId, placementContext);
            }
        }

        for (var tileX = -20; tileX <= 12; tileX++)
        {
            for (var tileY = 22; tileY <= 25; tileY++)
            {
                _worldRuntime.PlaceTile(new WorldTileCoord(tileX, tileY), DirtTileId, placementContext);
            }
        }

        for (var tileX = 29; tileX <= 35; tileX++)
        {
            for (var tileY = 18; tileY <= 19; tileY++)
            {
                _worldRuntime.PlaceTile(new WorldTileCoord(tileX, tileY), BrickTileId, placementContext);
            }
        }
    }

    private void EnsureVisibleSmokeChunksLoaded()
    {
        if (!_worldRuntime.IsPersistenceEnabled)
        {
            return;
        }

        foreach (var chunkCoord in _worldRenderer.GetVisibleChunkCoords())
        {
            _worldRuntime.EnsureChunkLoaded(chunkCoord);
        }
    }

    private void LogInitializationSummary(WorldMetadata metadata)
    {
        var leftCell = _worldRuntime.GetCell(new WorldTileCoord(-32, 26));
        var centerCell = _worldRuntime.GetCell(new WorldTileCoord(0, 26));
        var rightCell = _worldRuntime.GetCell(new WorldTileCoord(63, 26));
        var restoredCell = _worldRuntime.GetCell(
            new WorldTileCoord(10, 24),
            new QueryOptions { LoadChunkIfMissing = true });
        var restoredBridgeCell = _worldRuntime.GetCell(
            new WorldTileCoord(31, 18),
            new QueryOptions { LoadChunkIfMissing = true });

        EngineDiagnostics.Info(
            $"Smoke application initialized. World='{metadata.Name}', LoadedChunks={_worldRuntime.WorldData.LoadedChunkCount}, " +
            $"Mode={(_isNewWorld ? "Created" : "Loaded")}, Camera={_camera.GetWorldViewBounds()}, " +
            $"LeftVariant={leftCell.Variant}, CenterVariant={centerCell.Variant}, RightVariant={rightCell.Variant}, " +
            $"RestoredTile10x24={restoredCell.ForegroundTileId}, RestoredVariant10x24={restoredCell.Variant}, " +
            $"RestoredBridge31x18={restoredBridgeCell.ForegroundTileId}.");
    }

    private void UpdateSelectedTile(FrameInput input)
    {
        if (input.KeyWentDown(InputKey.D1))
        {
            SetSelectedTile(StoneTileId);
        }
        else if (input.KeyWentDown(InputKey.D2))
        {
            SetSelectedTile(DirtTileId);
        }
        else if (input.KeyWentDown(InputKey.D3))
        {
            SetSelectedTile(BrickTileId);
        }
    }

    private void SetSelectedTile(ushort tileId)
    {
        if (_selectedTileId == tileId)
        {
            return;
        }

        _selectedTileId = tileId;
        var tileName = _contentRegistry.GetTileDef(tileId).Name;
        EngineDiagnostics.Info($"Selected tile changed. Id={tileId}, Name={tileName}.");
    }

    private void UpdateDebugOverlayToggle(FrameInput input)
    {
        if (!input.KeyWentDown(InputKey.F1))
        {
            return;
        }

        _isDebugOverlayEnabled = !_isDebugOverlayEnabled;
        EngineDiagnostics.Info($"Debug overlay toggled. Enabled={_isDebugOverlayEnabled}.");
    }

    private void UpdateManualSave(FrameTime frameTime, FrameInput input)
    {
        if (!input.KeyWentDown(InputKey.F5))
        {
            return;
        }

        var savedChunkCount = _worldRuntime.SaveWorld();
        EngineDiagnostics.Info($"Manual world save completed. SavedDirtyChunks={savedChunkCount}.");
    }

    private void UpdateCamera(FrameTime frameTime, FrameInput input)
    {
        var horizontal = 0;
        var vertical = 0;

        if (input.IsKeyDown(InputKey.A) || input.IsKeyDown(InputKey.Left))
        {
            horizontal--;
        }

        if (input.IsKeyDown(InputKey.D) || input.IsKeyDown(InputKey.Right))
        {
            horizontal++;
        }

        if (input.IsKeyDown(InputKey.W) || input.IsKeyDown(InputKey.Up))
        {
            vertical--;
        }

        if (input.IsKeyDown(InputKey.S) || input.IsKeyDown(InputKey.Down))
        {
            vertical++;
        }

        if (horizontal == 0 && vertical == 0)
        {
            return;
        }

        var magnitude = Math.Sqrt((horizontal * horizontal) + (vertical * vertical));
        if (magnitude <= 0)
        {
            return;
        }

        var speedMultiplier = input.IsKeyDown(InputKey.LeftShift) || input.IsKeyDown(InputKey.RightShift)
            ? CameraSpeedBoostMultiplier
            : 1d;
        var frameDistance = CameraSpeedPixelsPerSecond * speedMultiplier * frameTime.ElapsedTime.TotalSeconds;
        var deltaX = (int)Math.Round((horizontal / magnitude) * frameDistance);
        var deltaY = (int)Math.Round((vertical / magnitude) * frameDistance);

        _camera.PositionPixels = new Int2(
            _camera.PositionPixels.X + deltaX,
            _camera.PositionPixels.Y + deltaY);
    }

    private void UpdateTileEdits(FrameInput input)
    {
        var hoveredTileCoord = _debugOverlayRenderer.TryGetHoveredTileCoord(_camera, input);
        if (hoveredTileCoord is not { } coord)
        {
            return;
        }

        if (input.LeftButton.WentDown)
        {
            var placeResult = _worldRuntime.PlaceTile(
                coord,
                _selectedTileId,
                new TilePlacementContext
                {
                    ActorEntityId = 1,
                    Source = PlacementSource.DebugTool
                });

            EngineDiagnostics.Info(
                $"PlaceTile requested. Success={placeResult.Success}, Coord={placeResult.Coord}, " +
                $"Previous={placeResult.PreviousTileId}, Current={placeResult.CurrentTileId}, Dirty={placeResult.DirtyFlagsApplied}.");
        }
        else if (input.RightButton.WentDown)
        {
            var breakResult = _worldRuntime.BreakTile(
                coord,
                new TileBreakContext
                {
                    ActorEntityId = 1,
                    Source = BreakSource.DebugTool
                });

            EngineDiagnostics.Info(
                $"BreakTile requested. Success={breakResult.Success}, Coord={breakResult.Coord}, " +
                $"Previous={breakResult.PreviousTileId}, Current={breakResult.CurrentTileId}, Dirty={breakResult.DirtyFlagsApplied}.");
        }
    }

}
