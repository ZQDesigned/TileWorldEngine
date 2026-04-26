namespace TileWorld.Engine.World.Cells;

/// <summary>
/// Defines the built-in liquid identifiers used by <see cref="TileCell"/>.
/// </summary>
public enum LiquidKind : byte
{
    /// <summary>
    /// No liquid is present in the cell.
    /// </summary>
    None = 0,

    /// <summary>
    /// Standard water liquid.
    /// </summary>
    Water = 1,

    /// <summary>
    /// Hot lava liquid.
    /// </summary>
    Lava = 2,

    /// <summary>
    /// Viscous honey liquid.
    /// </summary>
    Honey = 3
}
