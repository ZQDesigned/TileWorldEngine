using System;
using System.Globalization;

namespace TileWorld.Engine.Core.Math;

/// <summary>
/// Represents a two-dimensional floating-point vector in backend-neutral engine space.
/// </summary>
public readonly record struct Float2(float X, float Y)
{
    /// <summary>
    /// Gets the zero vector.
    /// </summary>
    public static Float2 Zero { get; } = new(0f, 0f);

    /// <summary>
    /// Gets the vector whose components are both one.
    /// </summary>
    public static Float2 One { get; } = new(1f, 1f);

    /// <summary>
    /// Adds two vectors.
    /// </summary>
    public static Float2 operator +(Float2 left, Float2 right)
    {
        return new Float2(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    /// Subtracts two vectors.
    /// </summary>
    public static Float2 operator -(Float2 left, Float2 right)
    {
        return new Float2(left.X - right.X, left.Y - right.Y);
    }

    /// <summary>
    /// Multiplies a vector by a scalar.
    /// </summary>
    public static Float2 operator *(Float2 value, float scalar)
    {
        return new Float2(value.X * scalar, value.Y * scalar);
    }

    /// <summary>
    /// Divides a vector by a scalar.
    /// </summary>
    public static Float2 operator /(Float2 value, float scalar)
    {
        return new Float2(value.X / scalar, value.Y / scalar);
    }

    /// <summary>
    /// Returns a culture-invariant string representation of the vector.
    /// </summary>
    public override string ToString()
    {
        return FormattableString.Invariant(
            $"({X.ToString(CultureInfo.InvariantCulture)}, {Y.ToString(CultureInfo.InvariantCulture)})");
    }
}
