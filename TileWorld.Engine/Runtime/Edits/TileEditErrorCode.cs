namespace TileWorld.Engine.Runtime.Edits;

/// <summary>
/// Enumerates failure reasons that can be returned by tile editing APIs.
/// </summary>
public enum TileEditErrorCode
{
    None = 0,
    ChunkNotLoaded = 1,
    OutOfBounds = 2,
    InvalidTileId = 3,
    ValidationFailed = 4,
    OccupiedByObject = 5,
    NoTilePresent = 6,
    TileNotMineable = 7,
    InternalError = 8
}
