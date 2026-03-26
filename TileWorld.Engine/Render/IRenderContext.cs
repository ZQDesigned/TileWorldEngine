using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Render;

/// <summary>
/// Defines the rendering operations exposed by a host backend to engine applications.
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// Gets the current viewport size in pixels.
    /// </summary>
    Int2 ViewportSizePixels { get; }

    /// <summary>
    /// Clears the current frame buffer to a solid color.
    /// </summary>
    /// <param name="color">The clear color.</param>
    void Clear(ColorRgba32 color);

    /// <summary>
    /// Submits a sprite draw command to the current frame.
    /// </summary>
    /// <param name="command">The backend-neutral sprite draw request.</param>
    void DrawSprite(SpriteDrawCommand command);
}
