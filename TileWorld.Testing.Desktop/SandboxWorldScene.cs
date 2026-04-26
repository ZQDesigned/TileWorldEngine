using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TileWorld.Engine.Content.Items;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Content.Biomes;
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
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Objects;
using TileWorld.Testing.Desktop.WorldGeneration;

namespace TileWorld.Testing.Desktop;

internal sealed class SandboxWorldScene : IEngineScene
{
    private const string DebugWhiteTextureKey = "debug/white";
    private const ushort StoneTileId = 1;
    private const ushort DirtTileId = 2;
    private const ushort BrickTileId = 3;
    private const ushort StoneWallId = 1;
    private const ushort DirtWallId = 2;
    private const ushort BrickWallId = 3;
    private const int CrateObjectDefId = 100;
    private const int BenchObjectDefId = 101;
    private const int LampObjectDefId = 102;
    private const int StoneBlockItemId = 1001;
    private const int DirtBlockItemId = 1002;
    private const int BrickBlockItemId = 1003;
    private const int CrateItemId = 2001;
    private const int BenchItemId = 2002;
    private const int LampItemId = 2003;
    private readonly ushort[] _tilePalette = [StoneTileId, DirtTileId, BrickTileId];
    private readonly ushort[] _wallPalette = [StoneWallId, DirtWallId, BrickWallId];
    private readonly int[] _objectPalette = [CrateObjectDefId, BenchObjectDefId, LampObjectDefId];
    private readonly Dictionary<int, int> _collectedItemCounts = new();
    private readonly DebugBitmapFont5x7 _font = new();
    private readonly Func<IEngineScene> _menuSceneFactory;
    private RectI _continueButtonRect;
    private DebugOverlayRenderer _debugOverlayRenderer = null!;
    private bool _isPauseMenuOpen;
    private FrameInput _lastFrameInput = FrameInput.Empty;
    private bool _isInitialized;
    private bool _isDebugOverlayEnabled = true;
    private bool _isNewWorld;
    private Camera2D _camera = null!;
    private ContentRegistry _contentRegistry = null!;
    private int _playerEntityId;
    private WorldRenderSettings _renderSettings = null!;
    private SceneHostApplication _sceneHost = null!;
    private int _selectedPaletteIndex;
    private int _selectedPauseButtonIndex;
    private ToolMode _toolMode = ToolMode.Tile;
    private RectI _returnToMenuButtonRect;
    private WorldRenderer _worldRenderer = null!;
    private WorldRuntime _worldRuntime = null!;
    private WorldMetadata _worldMetadata = null!;

    public SandboxWorldScene(string worldPath, Func<IEngineScene> menuSceneFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worldPath);
        _menuSceneFactory = menuSceneFactory ?? throw new ArgumentNullException(nameof(menuSceneFactory));
        WorldPath = worldPath;
    }

    public string WorldPath { get; }

    public void OnEnter(SceneHostApplication sceneHost)
    {
        if (_isInitialized)
        {
            return;
        }

        _sceneHost = sceneHost ?? throw new ArgumentNullException(nameof(sceneHost));
        _contentRegistry = new ContentRegistry();
        _renderSettings = new WorldRenderSettings();
        _debugOverlayRenderer = new DebugOverlayRenderer(_renderSettings);
        RegisterContent();

        var worldStorage = new WorldStorage();
        _worldMetadata = worldStorage.HasWorld(WorldPath)
            ? worldStorage.LoadMetadata(WorldPath)
            : new WorldMetadata
            {
                WorldId = Path.GetFileName(WorldPath),
                Name = Path.GetFileName(WorldPath),
                SpawnTile = new Int2(4, 18)
            };
        _isNewWorld = !HasPersistedChunkData(WorldPath);

        _worldRuntime = new WorldRuntime(
            new WorldData(_worldMetadata),
            _contentRegistry,
            new WorldRuntimeOptions
            {
                WorldPath = WorldPath,
                WorldStorage = worldStorage,
                SaveOnShutdown = true,
                EnableAutoSave = true,
                AutoSaveInterval = TimeSpan.FromSeconds(30),
                AutoSaveIdleDelay = TimeSpan.FromSeconds(4),
                MinimumAutoSaveSpacing = TimeSpan.FromSeconds(8),
                ActiveRadiusInChunks = 1,
                WorldGeneratorRegistry = DesktopWorldGeneratorRegistry.CreateDefault(),
                FallbackGeneratorId = DesktopWorldGeneratorIds.LegacyFlat
            });
        _camera = new Camera2D(new Int2(0, 0), new Int2(800, 480));
        _worldRenderer = new WorldRenderer(
            _camera,
            new ChunkRenderCacheBuilder(_contentRegistry, _renderSettings),
            _renderSettings);

        SubscribeDiagnostics();

        _worldRuntime.Initialize();

        if (TryResolvePersistedPlayer(out var persistedPlayer))
        {
            _playerEntityId = persistedPlayer.EntityId;
            EnsureSpawnAreaLoaded(new Int2((int)MathF.Floor(persistedPlayer.Position.X), (int)MathF.Floor(persistedPlayer.Position.Y)));
            EngineDiagnostics.Info(
                $"Sandbox world scene restored player state. EntityId={persistedPlayer.EntityId}, Position={persistedPlayer.Position}, Velocity={persistedPlayer.Velocity}.");
        }
        else
        {
            EnsureSpawnAreaLoaded(_worldMetadata.SpawnTile);
            _playerEntityId = _worldRuntime.SpawnPlayer(new Float2(_worldMetadata.SpawnTile.X + 0.5f, _worldMetadata.SpawnTile.Y - 1.95f));
        }

        UpdateCameraFromPlayer();
        LogInitializationSummary(_worldMetadata);
        _isInitialized = true;
    }

    public void OnExit()
    {
        if (!_isInitialized)
        {
            return;
        }

        _worldRuntime.Shutdown();
        EngineDiagnostics.Info($"Sandbox world scene shutdown. WorldPath={WorldPath}.");
        _isInitialized = false;
        _isDebugOverlayEnabled = true;
        _isPauseMenuOpen = false;
        _isNewWorld = false;
        _lastFrameInput = FrameInput.Empty;
        _selectedPaletteIndex = 0;
        _selectedPauseButtonIndex = 0;
        _toolMode = ToolMode.Tile;
        _playerEntityId = 0;
        _collectedItemCounts.Clear();
    }

    public void Update(FrameTime frameTime, FrameInput input)
    {
        if (!_isInitialized)
        {
            return;
        }

        _lastFrameInput = input ?? FrameInput.Empty;
        if (UpdatePauseMenu(input))
        {
            return;
        }

        UpdateToolSelection(input);
        UpdateDebugOverlayToggle(input);
        UpdatePlayerControl(input);
        UpdatePlayerLightSource();
        _worldRuntime.Update(frameTime);
        UpdateCameraFromPlayer();
        UpdateManualSave(input);
        UpdateWorldEdits(input);
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
        if (_isDebugOverlayEnabled && !_isPauseMenuOpen)
        {
            _debugOverlayRenderer.Draw(
                _worldRuntime,
                _worldRenderer,
                _camera,
                _lastFrameInput,
                renderContext,
                GetSelectedTileIdForOverlay(),
                GetSelectionLabel());
        }

        if (_isPauseMenuOpen)
        {
            DrawPauseOverlay(renderContext);
        }
    }

    private static bool HasPersistedChunkData(string worldPath)
    {
        var chunksPath = Path.Combine(worldPath, "chunks");
        return Directory.Exists(chunksPath) &&
               Directory.EnumerateFiles(chunksPath, "*.chk", SearchOption.TopDirectoryOnly).Any();
    }

    private void RegisterContent()
    {
        RegisterTileDefs();
        RegisterWallDefs();
        RegisterBiomeDefs();
        RegisterItemDefs();
        RegisterObjectDefs();
    }

    private void RegisterTileDefs()
    {
        _contentRegistry.RegisterTile(new TileDef
        {
            Id = StoneTileId,
            Name = "Stone",
            Category = "Terrain",
            IsSolid = true,
            BlocksLight = true,
            CanBeMined = true,
            Hardness = 1,
            BreakDropItemId = StoneBlockItemId,
            AutoTileGroupId = 1,
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(125, 125, 125), false)
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
            BreakDropItemId = DirtBlockItemId,
            AutoTileGroupId = 2,
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(139, 103, 68), false)
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
            BreakDropItemId = BrickBlockItemId,
            AutoTileGroupId = 3,
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(180, 70, 60), false)
        });
    }

    private void RegisterWallDefs()
    {
        _contentRegistry.RegisterWall(new WallDef
        {
            Id = StoneWallId,
            Name = "Stone Wall",
            AutoTileGroupId = 0,
            CountsAsRoomWall = true,
            ObscuresBackground = true,
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(110, 110, 110, 160), false)
        });
        _contentRegistry.RegisterWall(new WallDef
        {
            Id = DirtWallId,
            Name = "Dirt Wall",
            AutoTileGroupId = 0,
            CountsAsRoomWall = true,
            ObscuresBackground = true,
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(117, 88, 58, 155), false)
        });
        _contentRegistry.RegisterWall(new WallDef
        {
            Id = BrickWallId,
            Name = "Brick Wall",
            AutoTileGroupId = 0,
            CountsAsRoomWall = true,
            ObscuresBackground = true,
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(148, 52, 44, 170), false)
        });
    }

    private void RegisterBiomeDefs()
    {
        _contentRegistry.RegisterBiome(new BiomeDef
        {
            Id = 1,
            Name = "Plains",
            SurfaceTileId = DirtTileId,
            SubsurfaceTileId = StoneTileId,
            SurfaceWallId = DirtWallId,
            Priority = 5
        });
        _contentRegistry.RegisterBiome(new BiomeDef
        {
            Id = 2,
            Name = "Rocky",
            SurfaceTileId = StoneTileId,
            SubsurfaceTileId = StoneTileId,
            SurfaceWallId = StoneWallId,
            Priority = 10
        });
    }

    private void RegisterItemDefs()
    {
        _contentRegistry.RegisterItem(new ItemDef
        {
            Id = StoneBlockItemId,
            Name = "Stone Block",
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(125, 125, 125), false)
        });
        _contentRegistry.RegisterItem(new ItemDef
        {
            Id = DirtBlockItemId,
            Name = "Dirt Block",
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(139, 103, 68), false)
        });
        _contentRegistry.RegisterItem(new ItemDef
        {
            Id = BrickBlockItemId,
            Name = "Brick Block",
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(180, 70, 60), false)
        });
        _contentRegistry.RegisterItem(new ItemDef
        {
            Id = CrateItemId,
            Name = "Crate",
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(194, 138, 68), false)
        });
        _contentRegistry.RegisterItem(new ItemDef
        {
            Id = BenchItemId,
            Name = "Bench",
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(181, 145, 102), false)
        });
        _contentRegistry.RegisterItem(new ItemDef
        {
            Id = LampItemId,
            Name = "Lamp",
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(255, 227, 117), false)
        });
    }

    private void RegisterObjectDefs()
    {
        _contentRegistry.RegisterObject(new ObjectDef
        {
            Id = CrateObjectDefId,
            Name = "Crate",
            SizeInTiles = new Int2(2, 2),
            RequiresSupport = true,
            BreakDropItemId = CrateItemId,
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(194, 138, 68), false)
        });
        _contentRegistry.RegisterObject(new ObjectDef
        {
            Id = BenchObjectDefId,
            Name = "Bench",
            SizeInTiles = new Int2(3, 2),
            RequiresSupport = true,
            BreakDropItemId = BenchItemId,
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(181, 145, 102), false)
        });
        _contentRegistry.RegisterObject(new ObjectDef
        {
            Id = LampObjectDefId,
            Name = "Lamp",
            SizeInTiles = new Int2(1, 3),
            RequiresSupport = true,
            BreakDropItemId = LampItemId,
            EmissiveLight = 14,
            Visual = new TileVisualDef(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), new ColorRgba32(255, 227, 117), false)
        });
    }

    private void SubscribeDiagnostics()
    {
        _worldRuntime.Subscribe<TileChangedEvent>(evt =>
            EngineDiagnostics.Info($"TileChanged: Coord={evt.Coord}, Old={evt.OldTileId}, New={evt.NewTileId}"));
        _worldRuntime.Subscribe<TilePlacedEvent>(evt =>
            EngineDiagnostics.Info($"TilePlaced: Coord={evt.Coord}, Tile={evt.TileId}, Source={evt.Source}"));
        _worldRuntime.Subscribe<TileBrokenEvent>(evt =>
            EngineDiagnostics.Info($"TileBroken: Coord={evt.Coord}, Previous={evt.PreviousTileId}, Source={evt.Source}"));
        _worldRuntime.Subscribe<ObjectPlacedEvent>(evt =>
            EngineDiagnostics.Info($"ObjectPlaced: Instance={evt.ObjectInstanceId}, Def={evt.ObjectDefId}, Anchor={evt.AnchorCoord}."));
        _worldRuntime.Subscribe<ObjectRemovedEvent>(evt =>
            EngineDiagnostics.Info($"ObjectRemoved: Instance={evt.ObjectInstanceId}, Def={evt.ObjectDefId}, Anchor={evt.AnchorCoord}, Destroyed={evt.Destroyed}."));
        _worldRuntime.Subscribe<ChunkLoadedEvent>(evt =>
            EngineDiagnostics.Info($"ChunkLoaded: Coord={evt.Coord}, Source={evt.Source}."));
        _worldRuntime.Subscribe<ChunkQueuedEvent>(evt =>
            EngineDiagnostics.Trace($"ChunkQueued: Coord={evt.Coord}."));
        _worldRuntime.Subscribe<ChunkActivatedEvent>(evt =>
            EngineDiagnostics.Info($"ChunkActivated: Coord={evt.Coord}."));
        _worldRuntime.Subscribe<ChunkUnloadingEvent>(evt =>
            EngineDiagnostics.Info($"ChunkUnloading: Coord={evt.Coord}."));
        _worldRuntime.Subscribe<DropCollectedEvent>(evt =>
        {
            _collectedItemCounts.TryGetValue(evt.ItemDefId, out var count);
            _collectedItemCounts[evt.ItemDefId] = count + evt.Amount;
            EngineDiagnostics.Info(
                $"DropCollected: Item={evt.ItemDefId}, Collector={evt.CollectorEntityId}, Amount={evt.Amount}, Total={_collectedItemCounts[evt.ItemDefId]}.");
        });
    }

    private void EnsureSpawnAreaLoaded(Int2 spawnTile)
    {
        _worldRuntime.EnsureActiveAround(new WorldTileCoord(spawnTile.X, spawnTile.Y));
    }

    private void LogInitializationSummary(WorldMetadata metadata)
    {
        var spawnCell = _worldRuntime.GetCell(new WorldTileCoord(metadata.SpawnTile.X, metadata.SpawnTile.Y + 8));
        EngineDiagnostics.Info(
            $"Sandbox world scene initialized. World='{metadata.Name}', Mode={(_isNewWorld ? "Created" : "Loaded")}, WorldPath='{WorldPath}', " +
            $"Generator={metadata.GeneratorId}@{metadata.GeneratorVersion}, LoadedChunks={_worldRuntime.WorldData.LoadedChunkCount}, SpawnTile={metadata.SpawnTile}, SpawnGround={spawnCell.ForegroundTileId}, " +
            $"Selection={GetSelectionLabel()}.");
    }

    private bool TryResolvePersistedPlayer(out TileWorld.Engine.Runtime.Entities.Entity player)
    {
        player = _worldRuntime.EnumerateEntities()
            .Where(static entity => entity.Type == TileWorld.Engine.Runtime.Entities.EntityType.Player)
            .OrderBy(static entity => entity.EntityId)
            .FirstOrDefault()!;

        return player is not null;
    }

    private bool UpdatePauseMenu(FrameInput input)
    {
        if (input.KeyWentDown(InputKey.Escape))
        {
            _isPauseMenuOpen = !_isPauseMenuOpen;
            _selectedPauseButtonIndex = 0;
            EngineDiagnostics.Info($"Pause menu toggled. Open={_isPauseMenuOpen}.");
        }

        if (!_isPauseMenuOpen)
        {
            return false;
        }

        if (input.KeyWentDown(InputKey.Up) || input.KeyWentDown(InputKey.W) || input.KeyWentDown(InputKey.Left) || input.KeyWentDown(InputKey.A))
        {
            _selectedPauseButtonIndex = 0;
        }
        else if (input.KeyWentDown(InputKey.Down) || input.KeyWentDown(InputKey.S) || input.KeyWentDown(InputKey.Right) || input.KeyWentDown(InputKey.D))
        {
            _selectedPauseButtonIndex = 1;
        }

        if (input.KeyWentDown(InputKey.Enter))
        {
            ActivatePauseButton(_selectedPauseButtonIndex);
            return true;
        }

        if (input.LeftButton.WentDown && input.IsMouseInsideViewport)
        {
            if (_continueButtonRect.Contains(input.MouseScreenPositionPixels))
            {
                ActivatePauseButton(0);
                return true;
            }

            if (_returnToMenuButtonRect.Contains(input.MouseScreenPositionPixels))
            {
                ActivatePauseButton(1);
                return true;
            }
        }

        return true;
    }

    private void UpdateToolSelection(FrameInput input)
    {
        if (input.KeyWentDown(InputKey.D1))
        {
            SetSelectedPaletteIndex(0);
        }
        else if (input.KeyWentDown(InputKey.D2))
        {
            SetSelectedPaletteIndex(1);
        }
        else if (input.KeyWentDown(InputKey.D3))
        {
            SetSelectedPaletteIndex(2);
        }

        if (input.MouseWheelDelta > 0)
        {
            CycleToolMode(1);
        }
        else if (input.MouseWheelDelta < 0)
        {
            CycleToolMode(-1);
        }
    }

    private void SetSelectedPaletteIndex(int paletteIndex)
    {
        if (_selectedPaletteIndex == paletteIndex)
        {
            return;
        }

        _selectedPaletteIndex = paletteIndex;
        EngineDiagnostics.Info($"Selection changed: {GetSelectionLabel()}.");
    }

    private void CycleToolMode(int direction)
    {
        var nextValue = ((int)_toolMode + direction) % 3;
        if (nextValue < 0)
        {
            nextValue += 3;
        }

        var nextMode = (ToolMode)nextValue;
        if (nextMode == _toolMode)
        {
            return;
        }

        _toolMode = nextMode;
        EngineDiagnostics.Info($"Tool mode changed: {GetSelectionLabel()}.");
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

    private void UpdatePlayerControl(FrameInput input)
    {
        var moveAxis = 0f;
        if (input.IsKeyDown(InputKey.A) || input.IsKeyDown(InputKey.Left))
        {
            moveAxis -= 1f;
        }

        if (input.IsKeyDown(InputKey.D) || input.IsKeyDown(InputKey.Right))
        {
            moveAxis += 1f;
        }

        var jumpRequested = input.KeyWentDown(InputKey.W) || input.KeyWentDown(InputKey.Up);
        _worldRuntime.SetPlayerInput(_playerEntityId, moveAxis, jumpRequested);
    }

    private void UpdatePlayerLightSource()
    {
        _worldRuntime.SetPlayerHeldLightLevel(_playerEntityId, ResolveHeldLightLevel());
    }

    private void UpdateCameraFromPlayer()
    {
        if (!_worldRuntime.TryGetEntity(_playerEntityId, out var player))
        {
            return;
        }

        var playerCenterPixels = new Int2(
            (int)MathF.Round((player.Position.X + player.LocalBounds.X + (player.LocalBounds.Width / 2f)) * _renderSettings.TileSizePixels),
            (int)MathF.Round((player.Position.Y + player.LocalBounds.Y + (player.LocalBounds.Height / 2f)) * _renderSettings.TileSizePixels));

        _camera.PositionPixels = new Int2(
            playerCenterPixels.X - (_camera.ViewportSizePixels.X / 2),
            playerCenterPixels.Y - (_camera.ViewportSizePixels.Y / 2));

        _worldRuntime.EnsureActiveForTileArea(CreateCameraTileBounds(), _renderSettings.VisibleChunkPadding);
    }

    private void UpdateManualSave(FrameInput input)
    {
        if (!input.KeyWentDown(InputKey.F5))
        {
            return;
        }

        var savedChunkCount = _worldRuntime.SaveWorld();
        EngineDiagnostics.Info($"Manual world save completed. SavedDirtyChunks={savedChunkCount}.");
    }

    private void UpdateWorldEdits(FrameInput input)
    {
        var hoveredTileCoord = _debugOverlayRenderer.TryGetHoveredTileCoord(_camera, input);
        if (hoveredTileCoord is not { } coord)
        {
            return;
        }

        if (input.LeftButton.WentDown)
        {
            HandlePlace(coord);
        }
        else if (input.RightButton.WentDown)
        {
            HandleRemove(coord);
        }
    }

    private void HandlePlace(WorldTileCoord coord)
    {
        switch (_toolMode)
        {
            case ToolMode.Tile:
                var placeResult = _worldRuntime.PlaceTile(
                    coord,
                    _tilePalette[_selectedPaletteIndex],
                    new TilePlacementContext
                    {
                        ActorEntityId = _playerEntityId,
                        Source = PlacementSource.DebugTool
                    });
                EngineDiagnostics.Info(
                    $"PlaceTile requested. Success={placeResult.Success}, Coord={placeResult.Coord}, Previous={placeResult.PreviousTileId}, Current={placeResult.CurrentTileId}, Dirty={placeResult.DirtyFlagsApplied}.");
                break;

            case ToolMode.Wall:
                var wallPlaced = _worldRuntime.SetBackgroundWall(coord, _wallPalette[_selectedPaletteIndex]);
                EngineDiagnostics.Info($"SetBackgroundWall requested. Success={wallPlaced}, Coord={coord}, Wall={_wallPalette[_selectedPaletteIndex]}.");
                break;

            case ToolMode.Object:
                var objectResult = _worldRuntime.PlaceObject(
                    coord,
                    _objectPalette[_selectedPaletteIndex],
                    new ObjectPlacementContext
                    {
                        ActorEntityId = _playerEntityId,
                        Source = PlacementSource.DebugTool
                    });
                EngineDiagnostics.Info(
                    $"PlaceObject requested. Success={objectResult.Success}, Coord={objectResult.AnchorCoord}, ObjectDef={objectResult.ObjectDefId}, Instance={objectResult.ObjectInstanceId}, Dirty={objectResult.DirtyFlagsApplied}.");
                break;
        }
    }

    private void HandleRemove(WorldTileCoord coord)
    {
        switch (_toolMode)
        {
            case ToolMode.Tile:
                var breakResult = _worldRuntime.BreakTile(
                    coord,
                    new TileBreakContext
                    {
                        ActorEntityId = _playerEntityId,
                        Source = BreakSource.DebugTool,
                        SpawnDrops = true
                    });
                EngineDiagnostics.Info(
                    $"BreakTile requested. Success={breakResult.Success}, Coord={breakResult.Coord}, Previous={breakResult.PreviousTileId}, Current={breakResult.CurrentTileId}, Dirty={breakResult.DirtyFlagsApplied}.");
                break;

            case ToolMode.Wall:
                var wallRemoved = _worldRuntime.RemoveBackgroundWall(coord);
                EngineDiagnostics.Info($"RemoveBackgroundWall requested. Success={wallRemoved}, Coord={coord}.");
                break;

            case ToolMode.Object:
                var removed = _worldRuntime.TryGetObjectAt(coord, out var instance) &&
                              _worldRuntime.RemoveObject(instance.InstanceId);
                EngineDiagnostics.Info($"RemoveObject requested. Success={removed}, Coord={coord}.");
                break;
        }
    }

    private string GetSelectionLabel()
    {
        return _toolMode switch
        {
            ToolMode.Tile => $"TILE {_tilePalette[_selectedPaletteIndex].ToString(CultureInfo.InvariantCulture)} {_contentRegistry.GetTileDef(_tilePalette[_selectedPaletteIndex]).Name.ToUpperInvariant()}",
            ToolMode.Wall => $"WALL {_wallPalette[_selectedPaletteIndex].ToString(CultureInfo.InvariantCulture)} {_contentRegistry.GetWallDef(_wallPalette[_selectedPaletteIndex]).Name.ToUpperInvariant()}",
            ToolMode.Object => $"OBJECT {_objectPalette[_selectedPaletteIndex].ToString(CultureInfo.InvariantCulture)} {_contentRegistry.GetObjectDef(_objectPalette[_selectedPaletteIndex]).Name.ToUpperInvariant()}",
            _ => "UNKNOWN"
        };
    }

    private ushort GetSelectedTileIdForOverlay()
    {
        return _toolMode == ToolMode.Tile
            ? _tilePalette[_selectedPaletteIndex]
            : (ushort)0;
    }

    private byte ResolveHeldLightLevel()
    {
        return _toolMode switch
        {
            ToolMode.Tile when _contentRegistry.TryGetTileDef(_tilePalette[_selectedPaletteIndex], out var tileDef) => tileDef.EmissiveLight,
            ToolMode.Object when _contentRegistry.TryGetObjectDef(_objectPalette[_selectedPaletteIndex], out var objectDef) => objectDef.EmissiveLight,
            _ => 0
        };
    }

    private void ActivatePauseButton(int buttonIndex)
    {
        switch (buttonIndex)
        {
            case 0:
                _isPauseMenuOpen = false;
                EngineDiagnostics.Info("Pause menu action: continue game.");
                break;

            case 1:
                EngineDiagnostics.Info("Pause menu action: return to menu.");
                _sceneHost.SwitchScene(_menuSceneFactory());
                break;
        }
    }

    private void DrawPauseOverlay(IRenderContext renderContext)
    {
        var viewport = renderContext.ViewportSizePixels;
        DrawFilledRect(renderContext, new RectI(0, 0, viewport.X, viewport.Y), new ColorRgba32(8, 10, 14, 165), 0.85f);

        var panelWidth = Math.Min(420, viewport.X - 80);
        var panelHeight = 236;
        var panelRect = new RectI(
            (viewport.X - panelWidth) / 2,
            (viewport.Y - panelHeight) / 2,
            panelWidth,
            panelHeight);

        DrawFilledRect(renderContext, panelRect, new ColorRgba32(26, 34, 48, 240), 0.86f);
        DrawBorder(renderContext, panelRect, new ColorRgba32(210, 226, 255), 2, 0.87f);
        DrawText(renderContext, "GAME PAUSED", new Int2(panelRect.X + 24, panelRect.Y + 24), ColorRgba32.White, 0.88f);
        DrawText(renderContext, "PRESS ESC TO RESUME.", new Int2(panelRect.X + 24, panelRect.Y + 56), new ColorRgba32(198, 208, 225), 0.88f);
        DrawText(renderContext, "OR CHOOSE AN ACTION BELOW.", new Int2(panelRect.X + 24, panelRect.Y + 82), new ColorRgba32(198, 208, 225), 0.88f);

        _continueButtonRect = new RectI(panelRect.X + 24, panelRect.Y + 126, panelRect.Width - 48, 40);
        _returnToMenuButtonRect = new RectI(panelRect.X + 24, panelRect.Y + 178, panelRect.Width - 48, 40);

        DrawPauseButton(renderContext, _continueButtonRect, "CONTINUE GAME", _selectedPauseButtonIndex == 0, 0.89f);
        DrawPauseButton(renderContext, _returnToMenuButtonRect, "RETURN TO MENU", _selectedPauseButtonIndex == 1, 0.89f);
    }

    private void DrawPauseButton(IRenderContext renderContext, RectI rect, string label, bool isSelected, float layerDepth)
    {
        DrawFilledRect(renderContext, rect, isSelected ? new ColorRgba32(75, 112, 170) : new ColorRgba32(48, 58, 76), layerDepth);
        DrawBorder(renderContext, rect, isSelected ? new ColorRgba32(233, 240, 255) : new ColorRgba32(126, 141, 168), 2, layerDepth + 0.001f);
        var textWidth = _font.MeasureTextWidth(label);
        DrawText(
            renderContext,
            label,
            new Int2(rect.X + Math.Max(12, (rect.Width - textWidth) / 2), rect.Y + 12),
            ColorRgba32.White,
            layerDepth + 0.002f);
    }

    private void DrawFilledRect(IRenderContext renderContext, RectI rect, ColorRgba32 color, float layerDepth)
    {
        renderContext.DrawSprite(new SpriteDrawCommand(DebugWhiteTextureKey, new RectI(0, 0, 1, 1), rect, color, layerDepth));
    }

    private void DrawBorder(IRenderContext renderContext, RectI rect, ColorRgba32 color, int thickness, float layerDepth)
    {
        DrawFilledRect(renderContext, new RectI(rect.X, rect.Y, rect.Width, thickness), color, layerDepth);
        DrawFilledRect(renderContext, new RectI(rect.X, rect.Bottom - thickness, rect.Width, thickness), color, layerDepth);
        DrawFilledRect(renderContext, new RectI(rect.X, rect.Y, thickness, rect.Height), color, layerDepth);
        DrawFilledRect(renderContext, new RectI(rect.Right - thickness, rect.Y, thickness, rect.Height), color, layerDepth);
    }

    private void DrawText(IRenderContext renderContext, string text, Int2 topLeftPixels, ColorRgba32 tint, float layerDepth)
    {
        foreach (var command in _font.CreateDrawCommands(text, topLeftPixels, DebugWhiteTextureKey, tint, layerDepth))
        {
            renderContext.DrawSprite(command);
        }
    }

    private RectI CreateCameraTileBounds()
    {
        var worldViewBoundsPixels = _camera.GetWorldViewBounds();
        var minTileX = FloorDivide(worldViewBoundsPixels.Left, _renderSettings.TileSizePixels);
        var minTileY = FloorDivide(worldViewBoundsPixels.Top, _renderSettings.TileSizePixels);
        var maxTileX = FloorDivide(GetInclusiveRight(worldViewBoundsPixels), _renderSettings.TileSizePixels);
        var maxTileY = FloorDivide(GetInclusiveBottom(worldViewBoundsPixels), _renderSettings.TileSizePixels);

        return new RectI(
            minTileX,
            minTileY,
            (maxTileX - minTileX) + 1,
            (maxTileY - minTileY) + 1);
    }

    private static int GetInclusiveRight(RectI bounds)
    {
        return bounds.Width == 0 ? bounds.Left : bounds.Right - 1;
    }

    private static int GetInclusiveBottom(RectI bounds)
    {
        return bounds.Height == 0 ? bounds.Top : bounds.Bottom - 1;
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;

        if (remainder < 0)
        {
            quotient--;
        }

        return quotient;
    }

    private enum ToolMode
    {
        Tile = 0,
        Wall = 1,
        Object = 2
    }
}
