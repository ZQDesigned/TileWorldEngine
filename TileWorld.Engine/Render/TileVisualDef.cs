using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Render;

/// <summary>
/// Describes the visual data required to render a tile type.
/// </summary>
/// <param name="TextureKey">The host-defined texture identifier to sample from.</param>
/// <param name="SourceRect">The source rectangle for the base tile sprite.</param>
/// <param name="Tint">The multiplicative tint color applied during drawing.</param>
/// <param name="UseVariantHorizontalStrip">Whether autotile variants advance horizontally across the source texture.</param>
public readonly record struct TileVisualDef(
    string TextureKey,
    RectI SourceRect,
    ColorRgba32 Tint,
    bool UseVariantHorizontalStrip)
{
    /// <summary>
    /// Gets the default white-pixel visual used when no explicit tile visuals are provided.
    /// </summary>
    public static TileVisualDef Default => new(
        "debug/white",
        new RectI(0, 0, 1, 1),
        ColorRgba32.White,
        false);
}
