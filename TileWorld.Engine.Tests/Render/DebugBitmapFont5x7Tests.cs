using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Render;

namespace TileWorld.Engine.Tests.Render;

public sealed class DebugBitmapFont5x7Tests
{
    [Fact]
    public void Supports_ReturnsTrueForConfiguredGlyphs()
    {
        var font = new DebugBitmapFont5x7();

        Assert.True(font.Supports('A'));
        Assert.True(font.Supports('a'));
        Assert.True(font.Supports('5'));
        Assert.True(font.Supports(':'));
        Assert.True(font.Supports('?'));
        Assert.False(font.Supports('@'));
    }

    [Fact]
    public void CreateDrawCommands_SkipsUnsupportedCharactersSafely()
    {
        var font = new DebugBitmapFont5x7();

        var supportedOnly = font.CreateDrawCommands("A", Int2.Zero, "debug/white", ColorRgba32.White, 1f);
        var withUnsupported = font.CreateDrawCommands("A@", Int2.Zero, "debug/white", ColorRgba32.White, 1f);

        Assert.NotEmpty(supportedOnly);
        Assert.Equal(supportedOnly.Count, withUnsupported.Count);
        Assert.True(font.MeasureTextWidth("A@") > font.MeasureTextWidth("A"));
    }
}
