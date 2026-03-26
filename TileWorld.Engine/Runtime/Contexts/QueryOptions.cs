namespace TileWorld.Engine.Runtime.Contexts;

/// <summary>
/// Controls how world queries resolve missing data.
/// </summary>
public sealed class QueryOptions
{
    /// <summary>
    /// Gets the default query options used when no overrides are supplied.
    /// </summary>
    public static QueryOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether missing chunks may be loaded or created on demand.
    /// </summary>
    public bool LoadChunkIfMissing { get; init; }

    /// <summary>
    /// Gets a value reserved for future chunk activation policies.
    /// </summary>
    public bool AllowInactiveChunk { get; init; }

    /// <summary>
    /// Gets a value indicating whether callers expect default values instead of failures when data is missing.
    /// </summary>
    public bool ReturnDefaultWhenMissing { get; init; } = true;
}
