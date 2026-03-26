namespace TileWorld.Engine.World.Cells;

/// <summary>
/// Represents the stored data for a single world tile cell.
/// </summary>
public record struct TileCell
{
    public static TileCell Empty => default;

    public ushort ForegroundTileId { get; init; }

    public ushort BackgroundWallId { get; init; }

    public byte LiquidType { get; init; }

    public byte LiquidAmount { get; init; }

    public ushort Variant { get; init; }

    public ushort Flags { get; init; }
}
