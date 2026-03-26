namespace TileWorld.Engine.Core.Math;

/// <summary>
/// Represents an axis-aligned floating-point rectangle.
/// </summary>
public readonly record struct AabbF(float X, float Y, float Width, float Height)
{
    /// <summary>
    /// Gets the minimum X coordinate.
    /// </summary>
    public float Left => X;

    /// <summary>
    /// Gets the minimum Y coordinate.
    /// </summary>
    public float Top => Y;

    /// <summary>
    /// Gets the maximum X coordinate.
    /// </summary>
    public float Right => X + Width;

    /// <summary>
    /// Gets the maximum Y coordinate.
    /// </summary>
    public float Bottom => Y + Height;

    /// <summary>
    /// Returns a copy translated by the supplied offset.
    /// </summary>
    /// <param name="offset">The offset to apply.</param>
    /// <returns>The translated rectangle.</returns>
    public AabbF Translate(Float2 offset)
    {
        return new AabbF(X + offset.X, Y + offset.Y, Width, Height);
    }

    /// <summary>
    /// Returns whether this rectangle intersects another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to test.</param>
    /// <returns><see langword="true"/> when the rectangles overlap.</returns>
    public bool Intersects(AabbF other)
    {
        return Left < other.Right &&
               Right > other.Left &&
               Top < other.Bottom &&
               Bottom > other.Top;
    }
}
