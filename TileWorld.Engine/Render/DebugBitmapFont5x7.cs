using System;
using System.Collections.Generic;
using System.Globalization;
using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Render;

/// <summary>
/// Provides a tiny bitmap font used for debug overlays without a content pipeline font asset.
/// </summary>
public sealed class DebugBitmapFont5x7
{
    private const int GlyphHeight = 7;
    private const int GlyphWidth = 5;
    private static readonly RectI WhitePixelSourceRect = new(0, 0, 1, 1);
    private static readonly IReadOnlyDictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        ['A'] = ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['B'] = ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
        ['C'] = ["01110", "10001", "10000", "10000", "10000", "10001", "01110"],
        ['D'] = ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
        ['E'] = ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
        ['F'] = ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
        ['G'] = ["01110", "10001", "10000", "10111", "10001", "10001", "01110"],
        ['H'] = ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['I'] = ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
        ['J'] = ["00111", "00010", "00010", "00010", "00010", "10010", "01100"],
        ['K'] = ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
        ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
        ['M'] = ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
        ['N'] = ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
        ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['P'] = ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
        ['Q'] = ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
        ['R'] = ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
        ['S'] = ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
        ['T'] = ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
        ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['V'] = ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
        ['W'] = ["10001", "10001", "10001", "10101", "10101", "11011", "10001"],
        ['X'] = ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
        ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        ['Z'] = ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
        ['0'] = ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
        ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
        ['2'] = ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
        ['3'] = ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
        ['4'] = ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
        ['5'] = ["11111", "10000", "10000", "11110", "00001", "00001", "11110"],
        ['6'] = ["01110", "10000", "10000", "11110", "10001", "10001", "01110"],
        ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
        ['8'] = ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
        ['9'] = ["01110", "10001", "10001", "01111", "00001", "00001", "01110"],
        [':'] = ["00000", "00100", "00100", "00000", "00100", "00100", "00000"],
        [','] = ["00000", "00000", "00000", "00000", "00110", "00100", "01000"],
        ['-'] = ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
        ['.'] = ["00000", "00000", "00000", "00000", "00000", "00100", "00100"],
        ['='] = ["00000", "11111", "00000", "11111", "00000", "00000", "00000"]
    };

    /// <summary>
    /// Creates a bitmap font renderer with a fixed glyph scale and spacing.
    /// </summary>
    /// <param name="glyphScale">The scale applied to each source glyph pixel.</param>
    /// <param name="glyphSpacingPixels">The spacing inserted between glyphs and lines.</param>
    public DebugBitmapFont5x7(int glyphScale = 2, int glyphSpacingPixels = 1)
    {
        if (glyphScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(glyphScale), glyphScale, "Glyph scale must be positive.");
        }

        if (glyphSpacingPixels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(glyphSpacingPixels), glyphSpacingPixels, "Glyph spacing cannot be negative.");
        }

        GlyphScale = glyphScale;
        GlyphSpacingPixels = glyphSpacingPixels;
    }

    public int GlyphScale { get; }

    public int GlyphSpacingPixels { get; }

    /// <summary>
    /// Gets the horizontal advance for a single glyph including spacing.
    /// </summary>
    public int GlyphAdvancePixels => (GlyphWidth * GlyphScale) + GlyphSpacingPixels;

    /// <summary>
    /// Gets the total line height including spacing.
    /// </summary>
    public int LineHeightPixels => (GlyphHeight * GlyphScale) + GlyphSpacingPixels;

    /// <summary>
    /// Returns whether the font contains a glyph for the supplied character.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns><see langword="true"/> when the character is supported.</returns>
    public bool Supports(char character)
    {
        return character == ' ' || Glyphs.ContainsKey(Normalize(character));
    }

    /// <summary>
    /// Measures the width of a text string using the configured glyph metrics.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>The measured width in pixels.</returns>
    public int MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return (text.Length * GlyphAdvancePixels) - GlyphSpacingPixels;
    }

    /// <summary>
    /// Converts text into sprite draw commands using the debug bitmap font.
    /// </summary>
    /// <param name="text">The text to draw.</param>
    /// <param name="topLeftPixels">The top-left screen-space draw position.</param>
    /// <param name="textureKey">The texture key used for the source white pixel.</param>
    /// <param name="tint">The tint applied to generated glyph pixels.</param>
    /// <param name="layerDepth">The layer depth applied to all generated commands.</param>
    /// <returns>A list of sprite draw commands for the supplied text.</returns>
    public IReadOnlyList<SpriteDrawCommand> CreateDrawCommands(
        string text,
        Int2 topLeftPixels,
        string textureKey,
        ColorRgba32 tint,
        float layerDepth)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(textureKey);

        var commands = new List<SpriteDrawCommand>();
        var cursorX = topLeftPixels.X;

        foreach (var character in text)
        {
            if (character == ' ')
            {
                cursorX += GlyphAdvancePixels;
                continue;
            }

            if (!Glyphs.TryGetValue(Normalize(character), out var rows))
            {
                cursorX += GlyphAdvancePixels;
                continue;
            }

            for (var y = 0; y < rows.Length; y++)
            {
                var row = rows[y];
                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x] != '1')
                    {
                        continue;
                    }

                    commands.Add(new SpriteDrawCommand(
                        textureKey,
                        WhitePixelSourceRect,
                        new RectI(
                            cursorX + (x * GlyphScale),
                            topLeftPixels.Y + (y * GlyphScale),
                            GlyphScale,
                            GlyphScale),
                        tint,
                        layerDepth));
                }
            }

            cursorX += GlyphAdvancePixels;
        }

        return commands;
    }

    private static char Normalize(char character)
    {
        return char.ToUpper(character, CultureInfo.InvariantCulture);
    }
}
