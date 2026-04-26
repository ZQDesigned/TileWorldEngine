using System;

namespace TileWorld.Engine.Render;

/// <summary>
/// Represents a backend-neutral RGBA bitmap that hosts can upload as a texture resource.
/// </summary>
public sealed class TextureBitmapRgba32
{
    private readonly ColorRgba32[] _pixels;

    /// <summary>
    /// Creates a bitmap from packed row-major pixel data.
    /// </summary>
    /// <param name="width">The bitmap width in pixels.</param>
    /// <param name="height">The bitmap height in pixels.</param>
    /// <param name="pixels">The packed row-major pixels.</param>
    public TextureBitmapRgba32(int width, int height, ColorRgba32[] pixels)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Bitmap width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Bitmap height must be positive.");
        }

        ArgumentNullException.ThrowIfNull(pixels);

        if (pixels.Length != width * height)
        {
            throw new ArgumentException("Pixel data length must match width * height.", nameof(pixels));
        }

        Width = width;
        Height = height;
        _pixels = pixels;
    }

    /// <summary>
    /// Gets the bitmap width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the bitmap height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the packed row-major pixels.
    /// </summary>
    public ReadOnlySpan<ColorRgba32> Pixels => _pixels;

    /// <summary>
    /// Gets the color at a pixel position.
    /// </summary>
    /// <param name="x">The horizontal pixel coordinate.</param>
    /// <param name="y">The vertical pixel coordinate.</param>
    /// <returns>The pixel color.</returns>
    public ColorRgba32 GetPixel(int x, int y)
    {
        if ((uint)x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Bitmap X coordinate is outside the bitmap.");
        }

        if ((uint)y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Bitmap Y coordinate is outside the bitmap.");
        }

        return _pixels[(y * Width) + x];
    }
}
