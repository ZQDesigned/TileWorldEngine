using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Render;

/// <summary>
/// Describes a single sprite draw request in backend-neutral terms.
/// </summary>
/// <param name="TextureKey">The host-defined texture identifier to sample from.</param>
/// <param name="SourceRect">The source rectangle inside the texture.</param>
/// <param name="DestinationRectPixels">The destination rectangle in screen pixels passed to the render backend.</param>
/// <param name="Tint">The multiplicative tint color applied during drawing.</param>
/// <param name="LayerDepth">The layer depth used to sort draw order.</param>
public readonly record struct SpriteDrawCommand(
    string TextureKey,
    RectI SourceRect,
    RectI DestinationRectPixels,
    ColorRgba32 Tint,
    float LayerDepth);
