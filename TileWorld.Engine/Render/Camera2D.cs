using System;
using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Render;

/// <summary>
/// Represents a simple pixel-space camera used to view the tile world.
/// </summary>
public sealed class Camera2D
{
    private Int2 _viewportSizePixels;

    /// <summary>
    /// Creates a camera at the origin with an empty viewport.
    /// </summary>
    public Camera2D()
        : this(Int2.Zero, Int2.Zero)
    {
    }

    /// <summary>
    /// Creates a camera with an explicit world position and viewport.
    /// </summary>
    /// <param name="positionPixels">The camera position in world pixels.</param>
    /// <param name="viewportSizePixels">The viewport size in pixels.</param>
    public Camera2D(Int2 positionPixels, Int2 viewportSizePixels)
    {
        PositionPixels = positionPixels;
        ViewportSizePixels = viewportSizePixels;
    }

    /// <summary>
    /// Gets or sets the camera position in world pixels.
    /// </summary>
    public Int2 PositionPixels { get; set; }

    /// <summary>
    /// Gets or sets the viewport size in pixels.
    /// </summary>
    public Int2 ViewportSizePixels
    {
        get => _viewportSizePixels;
        set
        {
            if (value.X < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value.X, "Viewport width cannot be negative.");
            }

            if (value.Y < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value.Y, "Viewport height cannot be negative.");
            }

            _viewportSizePixels = value;
        }
    }

    /// <summary>
    /// Gets the current world-space view bounds covered by the camera.
    /// </summary>
    /// <returns>The camera view bounds in world pixels.</returns>
    public RectI GetWorldViewBounds()
    {
        return new RectI(PositionPixels.X, PositionPixels.Y, ViewportSizePixels.X, ViewportSizePixels.Y);
    }

    /// <summary>
    /// Converts a screen-space pixel coordinate into world-space pixels.
    /// </summary>
    /// <param name="screenPositionPixels">The screen-space coordinate.</param>
    /// <returns>The corresponding world-space coordinate.</returns>
    public Int2 ScreenToWorldPixels(Int2 screenPositionPixels)
    {
        return screenPositionPixels + PositionPixels;
    }

    /// <summary>
    /// Converts a world-space pixel coordinate into screen-space pixels.
    /// </summary>
    /// <param name="worldPositionPixels">The world-space coordinate.</param>
    /// <returns>The corresponding screen-space coordinate.</returns>
    public Int2 WorldToScreenPixels(Int2 worldPositionPixels)
    {
        return worldPositionPixels - PositionPixels;
    }
}
