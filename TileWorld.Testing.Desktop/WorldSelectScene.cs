using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Input;
using TileWorld.Engine.Render;
using TileWorld.Engine.Storage;
using TileWorld.Testing.Desktop.WorldGeneration;

namespace TileWorld.Testing.Desktop;

internal sealed class WorldSelectScene : IEngineScene
{
    private const string DebugWhiteTextureKey = "debug/white";
    private readonly DebugBitmapFont5x7 _font = new();
    private readonly Func<string, IEngineScene> _worldSceneFactory;
    private readonly WorldCatalog _worldCatalog = new();
    private CancellationTokenSource _createWorldCancellation = null!;
    private IReadOnlyList<WorldCatalogEntry> _worldEntries = [];
    private Task<string> _pendingCreateWorldTask;
    private RectI _createButtonRect;
    private bool _isInputPrimed;
    private RectI _openButtonRect;
    private Int2 _viewportSizePixels = new(1280, 720);
    private SceneHostApplication _sceneHost = null!;
    private int _selectedIndex;
    private string _statusMessage = "SELECT A WORLD OR CREATE A NEW ONE.";

    public WorldSelectScene(Func<string, IEngineScene> worldSceneFactory)
    {
        _worldSceneFactory = worldSceneFactory ?? throw new ArgumentNullException(nameof(worldSceneFactory));
    }

    public void OnEnter(SceneHostApplication sceneHost)
    {
        _sceneHost = sceneHost ?? throw new ArgumentNullException(nameof(sceneHost));
        _isInputPrimed = false;
        RefreshWorldEntries();
    }

    public void OnExit()
    {
        CancelPendingCreateWorldRequest();
        _createWorldCancellation?.Dispose();
        _createWorldCancellation = null!;
        _pendingCreateWorldTask = null;
    }

    public void Update(FrameTime frameTime, FrameInput input)
    {
        _ = frameTime;
        input ??= FrameInput.Empty;

        if (!_isInputPrimed)
        {
            if (!input.LeftButton.IsDown &&
                !input.IsKeyDown(InputKey.Enter) &&
                !input.IsKeyDown(InputKey.Escape) &&
                !input.IsKeyDown(InputKey.Up) &&
                !input.IsKeyDown(InputKey.Down))
            {
                _isInputPrimed = true;
            }

            return;
        }

        if (_pendingCreateWorldTask is not null)
        {
            UpdateCreateWorldFlow(input);
            return;
        }

        if (input.KeyWentDown(InputKey.Up))
        {
            MoveSelection(-1);
        }
        else if (input.KeyWentDown(InputKey.Down))
        {
            MoveSelection(1);
        }

        if (input.KeyWentDown(InputKey.Enter))
        {
            OpenSelectedWorld();
            return;
        }

        if (input.KeyWentDown(InputKey.Escape))
        {
            _sceneHost.HostServices.RequestExit();
            return;
        }

        if (input.LeftButton.WentDown && input.IsMouseInsideViewport)
        {
            HandleMouseClick(input.MouseScreenPositionPixels);
        }
    }

    public void Render(IRenderContext renderContext)
    {
        _viewportSizePixels = renderContext.ViewportSizePixels;
        renderContext.Clear(new ColorRgba32(22, 28, 40));

        var layout = CreateLayout(_viewportSizePixels);
        DrawFilledRect(renderContext, new RectI(0, 0, _viewportSizePixels.X, _viewportSizePixels.Y), new ColorRgba32(18, 24, 34), 0f);
        DrawFilledRect(renderContext, layout.ListPanelRect, new ColorRgba32(30, 38, 54), 0.05f);
        DrawBorder(renderContext, layout.ListPanelRect, new ColorRgba32(79, 100, 135), 2, 0.08f);
        DrawFilledRect(renderContext, layout.DetailsPanelRect, new ColorRgba32(30, 38, 54), 0.05f);
        DrawBorder(renderContext, layout.DetailsPanelRect, new ColorRgba32(79, 100, 135), 2, 0.08f);

        DrawText(renderContext, "WORLD SELECT", new Int2(layout.TitleRect.X, layout.TitleRect.Y), ColorRgba32.White, 0.1f);
        DrawText(renderContext, "OPEN AN EXISTING SAVE OR CREATE A NEW WORLD.", new Int2(layout.TitleRect.X, layout.TitleRect.Y + 28), new ColorRgba32(198, 208, 225), 0.1f);

        RenderWorldList(renderContext, layout);
        RenderSelectedWorldDetails(renderContext, layout);
        RenderButtons(renderContext, layout);
        RenderStatus(renderContext, layout);

        if (_pendingCreateWorldTask is not null)
        {
            RenderCreateWorldOverlay(renderContext, layout);
        }
    }

    private void UpdateCreateWorldFlow(FrameInput input)
    {
        if (input.KeyWentDown(InputKey.Escape))
        {
            CancelPendingCreateWorldRequest();
        }

        if (!_pendingCreateWorldTask.IsCompleted)
        {
            return;
        }

        string worldName;
        try
        {
            worldName = _pendingCreateWorldTask.GetAwaiter().GetResult();
        }
        finally
        {
            _pendingCreateWorldTask = null;
            _createWorldCancellation.Dispose();
            _createWorldCancellation = null!;
        }

        if (worldName is null)
        {
            _statusMessage = "WORLD CREATION CANCELED.";
            return;
        }

        if (string.IsNullOrWhiteSpace(worldName))
        {
            _statusMessage = "WORLD NAME CANNOT BE EMPTY.";
            return;
        }

        var createdWorld = _worldCatalog.CreateWorld(new WorldCreationOptions
        {
            Name = worldName.Trim(),
            GeneratorId = DesktopWorldGeneratorIds.Overworld,
            SpawnTile = new Int2(4, 18)
        });
        EngineDiagnostics.Info($"WorldSelectScene created new world. Name='{createdWorld.Name}', Directory='{createdWorld.DirectoryName}'.");
        _sceneHost.SwitchScene(_worldSceneFactory(createdWorld.WorldPath));
    }

    private void HandleMouseClick(Int2 mousePositionPixels)
    {
        if (_createButtonRect.Contains(mousePositionPixels))
        {
            BeginCreateWorld();
            return;
        }

        if (_openButtonRect.Contains(mousePositionPixels))
        {
            OpenSelectedWorld();
            return;
        }

        var layout = CreateLayout(_viewportSizePixels);
        var listBounds = GetListItemRects(layout);
        for (var index = 0; index < listBounds.Count; index++)
        {
            if (!listBounds[index].Contains(mousePositionPixels))
            {
                continue;
            }

            _selectedIndex = index;
            OpenSelectedWorld();
            return;
        }
    }

    private void OpenSelectedWorld()
    {
        if (_worldEntries.Count == 0)
        {
            _statusMessage = "NO WORLDS AVAILABLE. CREATE ONE TO BEGIN.";
            return;
        }

        var selectedWorld = _worldEntries[ClampSelectedIndex(_selectedIndex)];
        EngineDiagnostics.Info($"WorldSelectScene opening world. Name='{selectedWorld.Name}', Directory='{selectedWorld.DirectoryName}'.");
        _sceneHost.SwitchScene(_worldSceneFactory(selectedWorld.WorldPath));
    }

    private void BeginCreateWorld()
    {
        if (_pendingCreateWorldTask is not null)
        {
            return;
        }

        _createWorldCancellation = new CancellationTokenSource();
        _pendingCreateWorldTask = _sceneHost.HostServices.TextInput.RequestTextAsync(
            new TextInputRequest
            {
                Title = "Create World",
                Prompt = "Type a world name, then press Enter. Press Escape to cancel.",
                MaxLength = 48,
                CharacterFilter = IsAllowedWorldNameCharacter
            },
            _createWorldCancellation.Token);
        _statusMessage = "TEXT INPUT ACTIVE. ONLY DISPLAYABLE ASCII CHARACTERS ARE CURRENTLY ALLOWED.";
        EngineDiagnostics.Info("WorldSelectScene began world creation text input.");
    }

    private void CancelPendingCreateWorldRequest()
    {
        if (_pendingCreateWorldTask is null)
        {
            return;
        }

        _createWorldCancellation.Cancel();
    }

    private void RefreshWorldEntries()
    {
        _worldEntries = _worldCatalog.EnumerateWorlds();
        _selectedIndex = _worldEntries.Count == 0
            ? 0
            : ClampSelectedIndex(_selectedIndex);
        _statusMessage = _worldEntries.Count == 0
            ? "NO WORLDS FOUND. CREATE ONE TO GET STARTED."
            : $"FOUND {_worldEntries.Count.ToString(CultureInfo.InvariantCulture)} WORLDS.";
    }

    private void MoveSelection(int delta)
    {
        if (_worldEntries.Count == 0)
        {
            return;
        }

        _selectedIndex = (_selectedIndex + delta) % _worldEntries.Count;
        if (_selectedIndex < 0)
        {
            _selectedIndex += _worldEntries.Count;
        }
    }

    private void RenderWorldList(IRenderContext renderContext, Layout layout)
    {
        DrawText(renderContext, "AVAILABLE WORLDS", new Int2(layout.ListPanelRect.X + 16, layout.ListPanelRect.Y + 14), ColorRgba32.White, 0.1f);

        if (_worldEntries.Count == 0)
        {
            DrawText(renderContext, "NO WORLDS FOUND.", new Int2(layout.ListPanelRect.X + 16, layout.ListPanelRect.Y + 56), new ColorRgba32(226, 188, 104), 0.11f);
            DrawText(renderContext, "CLICK CREATE WORLD TO BEGIN.", new Int2(layout.ListPanelRect.X + 16, layout.ListPanelRect.Y + 84), new ColorRgba32(198, 208, 225), 0.11f);
            return;
        }

        var itemRects = GetListItemRects(layout);
        for (var index = 0; index < itemRects.Count && index < _worldEntries.Count; index++)
        {
            var entry = _worldEntries[index];
            var itemRect = itemRects[index];
            var isSelected = index == ClampSelectedIndex(_selectedIndex);

            DrawFilledRect(renderContext, itemRect, isSelected ? new ColorRgba32(70, 96, 144) : new ColorRgba32(38, 48, 68), 0.09f);
            DrawBorder(renderContext, itemRect, isSelected ? new ColorRgba32(198, 223, 255) : new ColorRgba32(80, 98, 126), 2, 0.1f);

            DrawText(renderContext, SanitizeForFont(entry.Name), new Int2(itemRect.X + 12, itemRect.Y + 10), ColorRgba32.White, 0.11f);
            DrawText(renderContext, $"DIR {SanitizeForFont(entry.DirectoryName)}", new Int2(itemRect.X + 12, itemRect.Y + 34), new ColorRgba32(198, 208, 225), 0.11f);
            DrawText(renderContext, FormatUtc(entry.LastWriteTimeUtc), new Int2(itemRect.X + itemRect.Width - 180, itemRect.Y + 34), new ColorRgba32(198, 208, 225), 0.11f);
        }
    }

    private void RenderSelectedWorldDetails(IRenderContext renderContext, Layout layout)
    {
        DrawText(renderContext, "WORLD DETAILS", new Int2(layout.DetailsPanelRect.X + 16, layout.DetailsPanelRect.Y + 14), ColorRgba32.White, 0.1f);

        if (_worldEntries.Count == 0)
        {
            DrawText(renderContext, "NO WORLD SELECTED.", new Int2(layout.DetailsPanelRect.X + 16, layout.DetailsPanelRect.Y + 56), new ColorRgba32(226, 188, 104), 0.11f);
            return;
        }

        var selectedWorld = _worldEntries[ClampSelectedIndex(_selectedIndex)];
        var cursorY = layout.DetailsPanelRect.Y + 56;
        DrawDetailLine(renderContext, layout.DetailsPanelRect.X + 16, ref cursorY, "NAME", selectedWorld.Name);
        DrawDetailLine(renderContext, layout.DetailsPanelRect.X + 16, ref cursorY, "DIRECTORY", selectedWorld.DirectoryName);
        DrawDetailLine(renderContext, layout.DetailsPanelRect.X + 16, ref cursorY, "WORLD ID", selectedWorld.WorldId);
        DrawDetailLine(renderContext, layout.DetailsPanelRect.X + 16, ref cursorY, "UPDATED", FormatUtc(selectedWorld.LastWriteTimeUtc));
        DrawDetailLine(renderContext, layout.DetailsPanelRect.X + 16, ref cursorY, "HAS CHUNKS", selectedWorld.HasChunkData ? "YES" : "NO");
    }

    private void RenderButtons(IRenderContext renderContext, Layout layout)
    {
        _openButtonRect = new RectI(layout.DetailsPanelRect.X + 16, layout.DetailsPanelRect.Bottom - 120, layout.DetailsPanelRect.Width - 32, 44);
        _createButtonRect = new RectI(layout.DetailsPanelRect.X + 16, layout.DetailsPanelRect.Bottom - 64, layout.DetailsPanelRect.Width - 32, 44);

        RenderButton(renderContext, _openButtonRect, "OPEN WORLD", _worldEntries.Count > 0);
        RenderButton(renderContext, _createButtonRect, "CREATE WORLD", true);
    }

    private void RenderStatus(IRenderContext renderContext, Layout layout)
    {
        DrawText(renderContext, SanitizeForFont(_statusMessage), new Int2(layout.ListPanelRect.X + 16, layout.ListPanelRect.Bottom + 16), new ColorRgba32(226, 188, 104), 0.11f);
    }

    private void RenderCreateWorldOverlay(IRenderContext renderContext, Layout layout)
    {
        var overlayRect = new RectI(layout.ListPanelRect.X + 48, layout.ListPanelRect.Y + 88, layout.ListPanelRect.Width + layout.DetailsPanelRect.Width - 96, 180);
        DrawFilledRect(renderContext, overlayRect, new ColorRgba32(20, 25, 34, 240), 0.2f);
        DrawBorder(renderContext, overlayRect, new ColorRgba32(214, 230, 255), 2, 0.21f);
        DrawText(renderContext, "CREATE WORLD", new Int2(overlayRect.X + 20, overlayRect.Y + 18), ColorRgba32.White, 0.22f);
        DrawText(renderContext, "TYPE A NAME WITH THE HOST TEXT INPUT SERVICE.", new Int2(overlayRect.X + 20, overlayRect.Y + 52), new ColorRgba32(198, 208, 225), 0.22f);
        DrawText(renderContext, "PRESS ENTER TO CONFIRM OR ESC TO CANCEL.", new Int2(overlayRect.X + 20, overlayRect.Y + 78), new ColorRgba32(198, 208, 225), 0.22f);

        var inputRect = new RectI(overlayRect.X + 20, overlayRect.Y + 112, overlayRect.Width - 40, 44);
        DrawFilledRect(renderContext, inputRect, new ColorRgba32(36, 44, 58), 0.22f);
        DrawBorder(renderContext, inputRect, new ColorRgba32(121, 146, 192), 2, 0.23f);
        var currentText = _sceneHost.HostServices.TextInput.CurrentText;
        DrawText(
            renderContext,
            string.IsNullOrWhiteSpace(currentText) ? "..." : SanitizeForFont(currentText),
            new Int2(inputRect.X + 12, inputRect.Y + 12),
            ColorRgba32.White,
            0.24f);
    }

    private void DrawDetailLine(IRenderContext renderContext, int x, ref int cursorY, string label, string value)
    {
        DrawText(renderContext, $"{label}:", new Int2(x, cursorY), new ColorRgba32(226, 188, 104), 0.11f);
        DrawText(renderContext, SanitizeForFont(value), new Int2(x, cursorY + 22), new ColorRgba32(198, 208, 225), 0.11f);
        cursorY += 56;
    }

    private void RenderButton(IRenderContext renderContext, RectI rect, string label, bool enabled)
    {
        DrawFilledRect(renderContext, rect, enabled ? new ColorRgba32(63, 112, 173) : new ColorRgba32(64, 68, 76), 0.12f);
        DrawBorder(renderContext, rect, enabled ? new ColorRgba32(214, 230, 255) : new ColorRgba32(110, 116, 126), 2, 0.13f);
        var textWidth = _font.MeasureTextWidth(label);
        DrawText(
            renderContext,
            label,
            new Int2(rect.X + Math.Max(12, (rect.Width - textWidth) / 2), rect.Y + 14),
            enabled ? ColorRgba32.White : new ColorRgba32(186, 190, 198),
            0.14f);
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

    private List<RectI> GetListItemRects(Layout layout)
    {
        var itemRects = new List<RectI>();
        var maxItems = Math.Max(1, (layout.ListPanelRect.Height - 72) / 72);
        var visibleItemCount = Math.Min(maxItems, _worldEntries.Count);

        for (var index = 0; index < visibleItemCount; index++)
        {
            itemRects.Add(new RectI(layout.ListPanelRect.X + 16, layout.ListPanelRect.Y + 48 + (index * 72), layout.ListPanelRect.Width - 32, 60));
        }

        return itemRects;
    }

    private Layout CreateLayout(Int2 viewportSizePixels)
    {
        var margin = 24;
        var titleRect = new RectI(margin, margin, viewportSizePixels.X - (margin * 2), 56);
        var contentTop = titleRect.Bottom + 12;
        var contentHeight = viewportSizePixels.Y - contentTop - 92;
        var listWidth = (int)MathF.Round((viewportSizePixels.X - (margin * 3)) * 0.58f);
        var detailsWidth = viewportSizePixels.X - (margin * 3) - listWidth;

        return new Layout(
            titleRect,
            new RectI(margin, contentTop, listWidth, contentHeight),
            new RectI(margin + listWidth + margin, contentTop, detailsWidth, contentHeight));
    }

    private int ClampSelectedIndex(int index)
    {
        if (_worldEntries.Count == 0)
        {
            return 0;
        }

        return Math.Clamp(index, 0, _worldEntries.Count - 1);
    }

    private string SanitizeForFont(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var characters = value.ToCharArray();
        for (var index = 0; index < characters.Length; index++)
        {
            if (!_font.Supports(characters[index]))
            {
                characters[index] = '?';
            }
        }

        return new string(characters);
    }

    private bool IsAllowedWorldNameCharacter(char character)
    {
        return _font.Supports(character);
    }

    private static string FormatUtc(DateTime utcTime)
    {
        return utcTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private readonly record struct Layout(RectI TitleRect, RectI ListPanelRect, RectI DetailsPanelRect);
}
