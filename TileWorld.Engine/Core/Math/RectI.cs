using System;

namespace TileWorld.Engine.Core.Math;

/// <summary>
/// Represents an axis-aligned integer rectangle.
/// </summary>
public readonly record struct RectI
{
    public RectI(int x, int y, int width, int height)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Rectangle width cannot be negative.");
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Rectangle height cannot be negative.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; init; }

    public int Y { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public int Left => X;

    public int Top => Y;

    public int Right => X + Width;

    public int Bottom => Y + Height;

    public bool Contains(Int2 point)
    {
        return Contains(point.X, point.Y);
    }

    public bool Contains(int x, int y)
    {
        return x >= Left && x < Right && y >= Top && y < Bottom;
    }

    public override string ToString()
    {
        return $"RectI({X}, {Y}, {Width}, {Height})";
    }
}
