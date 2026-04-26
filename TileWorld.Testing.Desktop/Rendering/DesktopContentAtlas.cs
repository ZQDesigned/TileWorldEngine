using System;
using TileWorld.Engine.Content.Items;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Render;

namespace TileWorld.Testing.Desktop.Rendering;

internal static class DesktopContentAtlas
{
    public const string TextureKey = "desktop/content-atlas";
    private const int TileSize = 16;

    public static readonly RectI StoneTileBaseRect = new(0, 0, TileSize, TileSize);
    public static readonly RectI DirtTileBaseRect = new(0, 16, TileSize, TileSize);
    public static readonly RectI BrickTileBaseRect = new(0, 32, TileSize, TileSize);

    public static readonly RectI StoneWallRect = new(0, 48, TileSize, TileSize);
    public static readonly RectI DirtWallRect = new(16, 48, TileSize, TileSize);
    public static readonly RectI BrickWallRect = new(32, 48, TileSize, TileSize);

    public static readonly RectI StoneBlockItemRect = new(0, 64, TileSize, TileSize);
    public static readonly RectI DirtBlockItemRect = new(16, 64, TileSize, TileSize);
    public static readonly RectI BrickBlockItemRect = new(32, 64, TileSize, TileSize);
    public static readonly RectI StoneWallItemRect = new(48, 64, TileSize, TileSize);
    public static readonly RectI DirtWallItemRect = new(64, 64, TileSize, TileSize);
    public static readonly RectI BrickWallItemRect = new(80, 64, TileSize, TileSize);
    public static readonly RectI CrateItemRect = new(96, 64, TileSize, TileSize);
    public static readonly RectI BenchItemRect = new(112, 64, TileSize, TileSize);
    public static readonly RectI LampItemRect = new(128, 64, TileSize, TileSize);

    public static readonly RectI CrateObjectRect = new(0, 80, 32, 32);
    public static readonly RectI BenchObjectRect = new(32, 80, 48, 32);
    public static readonly RectI LampObjectRect = new(80, 80, 16, 48);

    public static void EnsureRegistered(ITextureBitmapRegistry textures)
    {
        ArgumentNullException.ThrowIfNull(textures);

        if (textures.HasTexture(TextureKey))
        {
            return;
        }

        textures.RegisterTextureBitmap(TextureKey, CreateBitmap());
    }

    private static TextureBitmapRgba32 CreateBitmap()
    {
        var canvas = new BitmapCanvas(256, 128);

        DrawAutotileStrip(canvas, StoneTileBaseRect.Y, new Palette(
            new ColorRgba32(130, 130, 130),
            new ColorRgba32(112, 112, 112),
            new ColorRgba32(160, 160, 160),
            new ColorRgba32(92, 92, 92),
            ColorRgba32.White));
        DrawAutotileStrip(canvas, DirtTileBaseRect.Y, new Palette(
            new ColorRgba32(140, 102, 66),
            new ColorRgba32(118, 84, 53),
            new ColorRgba32(166, 127, 84),
            new ColorRgba32(89, 62, 39),
            ColorRgba32.White));
        DrawBrickStrip(canvas, BrickTileBaseRect.Y, new Palette(
            new ColorRgba32(177, 78, 66),
            new ColorRgba32(140, 57, 48),
            new ColorRgba32(206, 107, 94),
            new ColorRgba32(92, 38, 32),
            ColorRgba32.White));

        DrawWallTile(canvas, StoneWallRect, new Palette(
            new ColorRgba32(108, 108, 114, 192),
            new ColorRgba32(90, 90, 96, 192),
            new ColorRgba32(132, 132, 138, 192),
            new ColorRgba32(76, 76, 82, 192),
            new ColorRgba32(165, 165, 170, 170)));
        DrawWallTile(canvas, DirtWallRect, new Palette(
            new ColorRgba32(118, 87, 61, 188),
            new ColorRgba32(96, 68, 45, 188),
            new ColorRgba32(145, 108, 76, 188),
            new ColorRgba32(76, 52, 34, 188),
            new ColorRgba32(166, 132, 98, 164)));
        DrawWallTile(canvas, BrickWallRect, new Palette(
            new ColorRgba32(145, 58, 50, 196),
            new ColorRgba32(112, 43, 37, 196),
            new ColorRgba32(173, 84, 74, 196),
            new ColorRgba32(79, 30, 26, 196),
            new ColorRgba32(190, 112, 102, 176)));

        DrawBlockItem(canvas, StoneBlockItemRect, new Palette(
            new ColorRgba32(130, 130, 130),
            new ColorRgba32(112, 112, 112),
            new ColorRgba32(160, 160, 160),
            new ColorRgba32(92, 92, 92),
            ColorRgba32.White));
        DrawBlockItem(canvas, DirtBlockItemRect, new Palette(
            new ColorRgba32(140, 102, 66),
            new ColorRgba32(118, 84, 53),
            new ColorRgba32(166, 127, 84),
            new ColorRgba32(89, 62, 39),
            ColorRgba32.White));
        DrawBrickItem(canvas, BrickBlockItemRect, new Palette(
            new ColorRgba32(177, 78, 66),
            new ColorRgba32(140, 57, 48),
            new ColorRgba32(206, 107, 94),
            new ColorRgba32(92, 38, 32),
            ColorRgba32.White));

        DrawWallItem(canvas, StoneWallItemRect, new Palette(
            new ColorRgba32(108, 108, 114),
            new ColorRgba32(90, 90, 96),
            new ColorRgba32(132, 132, 138),
            new ColorRgba32(76, 76, 82),
            ColorRgba32.White));
        DrawWallItem(canvas, DirtWallItemRect, new Palette(
            new ColorRgba32(118, 87, 61),
            new ColorRgba32(96, 68, 45),
            new ColorRgba32(145, 108, 76),
            new ColorRgba32(76, 52, 34),
            ColorRgba32.White));
        DrawWallItem(canvas, BrickWallItemRect, new Palette(
            new ColorRgba32(145, 58, 50),
            new ColorRgba32(112, 43, 37),
            new ColorRgba32(173, 84, 74),
            new ColorRgba32(79, 30, 26),
            ColorRgba32.White));

        DrawCrateObject(canvas, CrateObjectRect);
        DrawBenchObject(canvas, BenchObjectRect);
        DrawLampObject(canvas, LampObjectRect);

        DrawCrateItem(canvas, CrateItemRect);
        DrawBenchItem(canvas, BenchItemRect);
        DrawLampItem(canvas, LampItemRect);

        return new TextureBitmapRgba32(canvas.Width, canvas.Height, canvas.Pixels);
    }

    private static void DrawAutotileStrip(BitmapCanvas canvas, int y, Palette palette)
    {
        for (var variant = 0; variant < 16; variant++)
        {
            DrawAutotileVariant(canvas, variant * TileSize, y, palette, variant);
        }
    }

    private static void DrawBrickStrip(BitmapCanvas canvas, int y, Palette palette)
    {
        for (var variant = 0; variant < 16; variant++)
        {
            DrawAutotileVariant(canvas, variant * TileSize, y, palette, variant, brickMode: true);
        }
    }

    private static void DrawAutotileVariant(BitmapCanvas canvas, int originX, int originY, Palette palette, int variant, bool brickMode = false)
    {
        for (var localY = 0; localY < TileSize; localY++)
        {
            for (var localX = 0; localX < TileSize; localX++)
            {
                var color = ResolveBasePatternColor(localX, localY, originX, originY, palette, brickMode);
                canvas.SetPixel(originX + localX, originY + localY, color);
            }
        }

        var hasUp = (variant & 1) != 0;
        var hasRight = (variant & 2) != 0;
        var hasDown = (variant & 4) != 0;
        var hasLeft = (variant & 8) != 0;

        if (!hasUp)
        {
            canvas.FillRect(new RectI(originX, originY, TileSize, 2), palette.Edge);
        }

        if (!hasRight)
        {
            canvas.FillRect(new RectI(originX + TileSize - 2, originY, 2, TileSize), palette.Edge);
        }

        if (!hasDown)
        {
            canvas.FillRect(new RectI(originX, originY + TileSize - 2, TileSize, 2), palette.Edge);
        }

        if (!hasLeft)
        {
            canvas.FillRect(new RectI(originX, originY, 2, TileSize), palette.Edge);
        }

        if (!hasUp && !hasLeft)
        {
            canvas.FillRect(new RectI(originX, originY, 4, 4), palette.Highlight);
        }

        if (!hasUp && !hasRight)
        {
            canvas.FillRect(new RectI(originX + TileSize - 4, originY, 4, 4), palette.Highlight);
        }

        if (!hasDown && !hasLeft)
        {
            canvas.FillRect(new RectI(originX, originY + TileSize - 4, 4, 4), palette.Shadow);
        }

        if (!hasDown && !hasRight)
        {
            canvas.FillRect(new RectI(originX + TileSize - 4, originY + TileSize - 4, 4, 4), palette.Shadow);
        }
    }

    private static ColorRgba32 ResolveBasePatternColor(int localX, int localY, int originX, int originY, Palette palette, bool brickMode)
    {
        var value = ((originX / TileSize) * 31) + (localX * 11) + (localY * 17) + (originY * 7);
        if (brickMode)
        {
            var mortarLine = localY % 5 == 0 || ((localY / 5) % 2 == 0 ? localX % 8 == 0 : (localX + 4) % 8 == 0);
            if (mortarLine)
            {
                return palette.Edge;
            }
        }

        if (value % 13 == 0)
        {
            return palette.Highlight;
        }

        if (value % 7 == 0)
        {
            return palette.Shadow;
        }

        return palette.Base;
    }

    private static void DrawWallTile(BitmapCanvas canvas, RectI rect, Palette palette)
    {
        canvas.FillRect(rect, new ColorRgba32(0, 0, 0, 0));
        for (var y = 1; y < rect.Height - 1; y++)
        {
            for (var x = 1; x < rect.Width - 1; x++)
            {
                var value = (x * 5) + (y * 9) + rect.X;
                var color = value % 9 == 0 ? palette.Highlight : value % 5 == 0 ? palette.Shadow : palette.Base;
                canvas.SetPixel(rect.X + x, rect.Y + y, color);
            }
        }

        canvas.DrawRect(rect, palette.Edge);
    }

    private static void DrawBlockItem(BitmapCanvas canvas, RectI rect, Palette palette)
    {
        var itemRect = Inset(rect, 2);
        canvas.FillRect(itemRect, palette.Base);
        canvas.DrawRect(itemRect, palette.Edge);
        canvas.FillRect(new RectI(itemRect.X + 1, itemRect.Y + 1, itemRect.Width - 2, 2), palette.Highlight);
        canvas.FillRect(new RectI(itemRect.X + 1, itemRect.Y + itemRect.Height - 3, itemRect.Width - 2, 2), palette.Shadow);
    }

    private static void DrawBrickItem(BitmapCanvas canvas, RectI rect, Palette palette)
    {
        DrawBlockItem(canvas, rect, palette);
        for (var y = rect.Y + 4; y < rect.Bottom - 3; y += 4)
        {
            canvas.FillRect(new RectI(rect.X + 3, y, rect.Width - 6, 1), palette.Edge);
        }
    }

    private static void DrawWallItem(BitmapCanvas canvas, RectI rect, Palette palette)
    {
        var itemRect = Inset(rect, 2);
        canvas.FillRect(itemRect, new ColorRgba32(0, 0, 0, 0));
        canvas.DrawRect(itemRect, palette.Edge);
        canvas.FillRect(new RectI(itemRect.X + 2, itemRect.Y + 2, itemRect.Width - 4, itemRect.Height - 4), palette.Base);
        canvas.FillRect(new RectI(itemRect.X + 3, itemRect.Y + 3, itemRect.Width - 6, 2), palette.Highlight);
    }

    private static void DrawCrateObject(BitmapCanvas canvas, RectI rect)
    {
        var palette = new Palette(
            new ColorRgba32(175, 124, 69),
            new ColorRgba32(146, 99, 50),
            new ColorRgba32(206, 161, 98),
            new ColorRgba32(110, 74, 35),
            ColorRgba32.White);
        canvas.FillRect(rect, palette.Base);
        canvas.DrawRect(rect, palette.Edge);
        canvas.FillRect(new RectI(rect.X + 3, rect.Y + 3, rect.Width - 6, 2), palette.Highlight);
        canvas.FillRect(new RectI(rect.X + 3, rect.Bottom - 5, rect.Width - 6, 2), palette.Shadow);
        canvas.FillRect(new RectI(rect.X + (rect.Width / 2) - 1, rect.Y + 2, 2, rect.Height - 4), palette.Edge);
        canvas.FillRect(new RectI(rect.X + 2, rect.Y + (rect.Height / 2) - 1, rect.Width - 4, 2), palette.Edge);
    }

    private static void DrawBenchObject(BitmapCanvas canvas, RectI rect)
    {
        var wood = new Palette(
            new ColorRgba32(173, 135, 92),
            new ColorRgba32(147, 111, 74),
            new ColorRgba32(204, 168, 124),
            new ColorRgba32(116, 82, 50),
            ColorRgba32.White);
        canvas.FillRect(rect, ColorRgba32.Transparent);
        canvas.FillRect(new RectI(rect.X + 2, rect.Y + 10, rect.Width - 4, 6), wood.Base);
        canvas.FillRect(new RectI(rect.X + 4, rect.Y + 4, rect.Width - 8, 6), wood.Highlight);
        canvas.DrawRect(new RectI(rect.X + 2, rect.Y + 10, rect.Width - 4, 6), wood.Edge);
        canvas.DrawRect(new RectI(rect.X + 4, rect.Y + 4, rect.Width - 8, 6), wood.Edge);
        canvas.FillRect(new RectI(rect.X + 6, rect.Y + 16, 4, rect.Height - 16), wood.Shadow);
        canvas.FillRect(new RectI(rect.Right - 10, rect.Y + 16, 4, rect.Height - 16), wood.Shadow);
    }

    private static void DrawLampObject(BitmapCanvas canvas, RectI rect)
    {
        canvas.FillRect(rect, ColorRgba32.Transparent);
        var metal = new Palette(
            new ColorRgba32(126, 134, 146),
            new ColorRgba32(95, 102, 112),
            new ColorRgba32(170, 178, 188),
            new ColorRgba32(71, 77, 86),
            ColorRgba32.White);
        var glow = new ColorRgba32(255, 231, 122, 220);
        canvas.FillRect(new RectI(rect.X + 6, rect.Y + 16, 4, rect.Height - 20), metal.Base);
        canvas.FillRect(new RectI(rect.X + 4, rect.Bottom - 6, 8, 4), metal.Shadow);
        canvas.FillRect(new RectI(rect.X + 3, rect.Y + 4, 10, 10), glow);
        canvas.DrawRect(new RectI(rect.X + 3, rect.Y + 4, 10, 10), new ColorRgba32(208, 170, 68));
        canvas.FillRect(new RectI(rect.X + 5, rect.Y + 6, 6, 6), new ColorRgba32(255, 247, 180));
    }

    private static void DrawCrateItem(BitmapCanvas canvas, RectI rect)
    {
        var objectRect = new RectI(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
        DrawCrateObject(canvas, objectRect);
    }

    private static void DrawBenchItem(BitmapCanvas canvas, RectI rect)
    {
        canvas.FillRect(rect, ColorRgba32.Transparent);
        var seat = new RectI(rect.X + 1, rect.Y + 6, rect.Width - 2, 4);
        canvas.FillRect(seat, new ColorRgba32(173, 135, 92));
        canvas.DrawRect(seat, new ColorRgba32(116, 82, 50));
        canvas.FillRect(new RectI(rect.X + 3, rect.Y + 10, 2, 4), new ColorRgba32(116, 82, 50));
        canvas.FillRect(new RectI(rect.Right - 5, rect.Y + 10, 2, 4), new ColorRgba32(116, 82, 50));
    }

    private static void DrawLampItem(BitmapCanvas canvas, RectI rect)
    {
        canvas.FillRect(rect, ColorRgba32.Transparent);
        canvas.FillRect(new RectI(rect.X + 7, rect.Y + 5, 2, 8), new ColorRgba32(118, 126, 136));
        canvas.FillRect(new RectI(rect.X + 4, rect.Y + 2, 8, 6), new ColorRgba32(255, 236, 132, 235));
        canvas.DrawRect(new RectI(rect.X + 4, rect.Y + 2, 8, 6), new ColorRgba32(208, 170, 68));
    }

    private static RectI Inset(RectI rect, int inset)
    {
        return new RectI(rect.X + inset, rect.Y + inset, rect.Width - (inset * 2), rect.Height - (inset * 2));
    }

    private readonly record struct Palette(
        ColorRgba32 Base,
        ColorRgba32 Shadow,
        ColorRgba32 Highlight,
        ColorRgba32 Edge,
        ColorRgba32 Accent);

    private sealed class BitmapCanvas
    {
        private readonly ColorRgba32[] _pixels;

        public BitmapCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new ColorRgba32[width * height];
        }

        public int Width { get; }

        public int Height { get; }

        public ColorRgba32[] Pixels => _pixels;

        public void SetPixel(int x, int y, ColorRgba32 color)
        {
            if ((uint)x >= Width || (uint)y >= Height)
            {
                return;
            }

            _pixels[(y * Width) + x] = color;
        }

        public void FillRect(RectI rect, ColorRgba32 color)
        {
            for (var y = rect.Top; y < rect.Bottom; y++)
            {
                for (var x = rect.Left; x < rect.Right; x++)
                {
                    SetPixel(x, y, color);
                }
            }
        }

        public void DrawRect(RectI rect, ColorRgba32 color)
        {
            FillRect(new RectI(rect.X, rect.Y, rect.Width, 1), color);
            FillRect(new RectI(rect.X, rect.Bottom - 1, rect.Width, 1), color);
            FillRect(new RectI(rect.X, rect.Y, 1, rect.Height), color);
            FillRect(new RectI(rect.Right - 1, rect.Y, 1, rect.Height), color);
        }
    }
}
