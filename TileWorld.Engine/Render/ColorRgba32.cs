namespace TileWorld.Engine.Render;

/// <summary>
/// Represents a 32-bit RGBA color value without depending on a graphics backend type.
/// </summary>
/// <param name="R">The red channel.</param>
/// <param name="G">The green channel.</param>
/// <param name="B">The blue channel.</param>
/// <param name="A">The alpha channel.</param>
public readonly record struct ColorRgba32(byte R, byte G, byte B, byte A = byte.MaxValue)
{
    /// <summary>
    /// Gets an opaque white color.
    /// </summary>
    public static ColorRgba32 White => new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

    /// <summary>
    /// Gets a fully transparent color.
    /// </summary>
    public static ColorRgba32 Transparent => new(0, 0, 0, 0);

    /// <summary>
    /// Gets the classic MonoGame/XNA cornflower blue clear color reproduced in engine-owned form.
    /// </summary>
    public static ColorRgba32 CornflowerBlue => new(100, 149, 237, byte.MaxValue);
}
