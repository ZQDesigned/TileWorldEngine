namespace TileWorld.Engine.World.Cells;

/// <summary>
/// Represents the resolved liquid state of one world tile cell.
/// </summary>
public readonly record struct LiquidState
{
    /// <summary>
    /// Gets an empty liquid state.
    /// </summary>
    public static LiquidState None => new(LiquidKind.None, 0);

    /// <summary>
    /// Creates a liquid state value.
    /// </summary>
    /// <param name="kind">The liquid kind.</param>
    /// <param name="amount">The liquid amount in the range <c>0..255</c>.</param>
    public LiquidState(LiquidKind kind, byte amount)
    {
        Kind = amount == 0 ? LiquidKind.None : kind == LiquidKind.None ? LiquidKind.Water : kind;
        Amount = amount;
    }

    /// <summary>
    /// Gets the liquid kind.
    /// </summary>
    public LiquidKind Kind { get; }

    /// <summary>
    /// Gets the liquid amount in the range <c>0..255</c>.
    /// </summary>
    public byte Amount { get; }

    /// <summary>
    /// Gets a value indicating whether this state contains liquid.
    /// </summary>
    public bool HasLiquid => Kind != LiquidKind.None && Amount > 0;
}
