namespace TileWorld.Engine.Runtime.Objects;

/// <summary>
/// Represents the failure reason for an object placement request.
/// </summary>
public enum ObjectPlacementErrorCode
{
    /// <summary>
    /// No error occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// The requested object definition identifier is invalid.
    /// </summary>
    InvalidObjectDefId = 1,

    /// <summary>
    /// The target footprint is already occupied.
    /// </summary>
    Occupied = 2,

    /// <summary>
    /// General validation failed.
    /// </summary>
    ValidationFailed = 3,

    /// <summary>
    /// The target footprint does not have the required support.
    /// </summary>
    MissingSupport = 4,

    /// <summary>
    /// The request could not be completed because the target anchor was invalid.
    /// </summary>
    InvalidAnchor = 5,

    /// <summary>
    /// The requested object instance could not be found.
    /// </summary>
    ObjectNotFound = 6,

    /// <summary>
    /// An unexpected internal error prevented the request from completing.
    /// </summary>
    InternalError = 7,

    /// <summary>
    /// The requested footprint extends outside the world's optional vertical bounds.
    /// </summary>
    OutOfBounds = 8
}
