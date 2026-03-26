namespace TileWorld.Engine.Core.Math;

/// <summary>
/// Represents a pair of integer coordinates or dimensions.
/// </summary>
public readonly record struct Int2(int X, int Y)
{
    public static Int2 Zero => new(0, 0);

    public static Int2 One => new(1, 1);

    public static Int2 operator +(Int2 left, Int2 right)
    {
        return new Int2(left.X + right.X, left.Y + right.Y);
    }

    public static Int2 operator -(Int2 left, Int2 right)
    {
        return new Int2(left.X - right.X, left.Y - right.Y);
    }

    public override string ToString()
    {
        return $"Int2({X}, {Y})";
    }
}
