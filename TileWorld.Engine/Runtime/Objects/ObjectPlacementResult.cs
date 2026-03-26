using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Objects;

/// <summary>
/// Represents the outcome of an object placement request.
/// </summary>
/// <param name="Success">Whether the placement completed successfully.</param>
/// <param name="ErrorCode">The failure reason when <paramref name="Success"/> is <see langword="false"/>.</param>
/// <param name="ObjectInstanceId">The created object instance identifier when placement succeeds.</param>
/// <param name="ObjectDefId">The placed object definition identifier.</param>
/// <param name="AnchorCoord">The logical anchor coordinate targeted by the request.</param>
/// <param name="DirtyFlagsApplied">The dirty flags applied to affected chunks.</param>
public readonly record struct ObjectPlacementResult(
    bool Success,
    ObjectPlacementErrorCode ErrorCode,
    int ObjectInstanceId,
    int ObjectDefId,
    WorldTileCoord AnchorCoord,
    ChunkDirtyFlags DirtyFlagsApplied)
{
    /// <summary>
    /// Creates a failed object-placement result.
    /// </summary>
    /// <param name="errorCode">The failure reason.</param>
    /// <param name="objectDefId">The requested object definition identifier.</param>
    /// <param name="anchorCoord">The logical anchor coordinate targeted by the request.</param>
    /// <returns>A failed object-placement result.</returns>
    public static ObjectPlacementResult Failed(
        ObjectPlacementErrorCode errorCode,
        int objectDefId,
        WorldTileCoord anchorCoord)
    {
        return new ObjectPlacementResult(false, errorCode, 0, objectDefId, anchorCoord, ChunkDirtyFlags.None);
    }

    /// <summary>
    /// Creates a successful object-placement result.
    /// </summary>
    /// <param name="objectInstanceId">The created object instance identifier.</param>
    /// <param name="objectDefId">The placed object definition identifier.</param>
    /// <param name="anchorCoord">The logical anchor coordinate targeted by the request.</param>
    /// <param name="dirtyFlagsApplied">The dirty flags applied to affected chunks.</param>
    /// <returns>A successful object-placement result.</returns>
    public static ObjectPlacementResult Succeeded(
        int objectInstanceId,
        int objectDefId,
        WorldTileCoord anchorCoord,
        ChunkDirtyFlags dirtyFlagsApplied)
    {
        return new ObjectPlacementResult(true, ObjectPlacementErrorCode.None, objectInstanceId, objectDefId, anchorCoord, dirtyFlagsApplied);
    }
}
