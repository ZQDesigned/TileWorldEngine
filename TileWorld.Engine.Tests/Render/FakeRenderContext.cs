using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Render;

namespace TileWorld.Engine.Tests.Render;

internal sealed class FakeRenderContext : IRenderContext
{
    public FakeRenderContext(Int2 viewportSizePixels)
    {
        ViewportSizePixels = viewportSizePixels;
    }

    public Int2 ViewportSizePixels { get; }

    public List<ColorRgba32> ClearCalls { get; } = [];

    public List<SpriteDrawCommand> DrawCalls { get; } = [];

    public void Clear(ColorRgba32 color)
    {
        ClearCalls.Add(color);
    }

    public void DrawSprite(SpriteDrawCommand command)
    {
        DrawCalls.Add(command);
    }
}
