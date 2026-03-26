using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Describes a new persisted world that should be created by <see cref="WorldCatalog"/>.
/// </summary>
public sealed class WorldCreationOptions
{
    /// <summary>
    /// Gets the display name for the new world.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the initial spawn tile for the new world.
    /// </summary>
    public Int2 SpawnTile { get; init; } = new(4, 18);

    /// <summary>
    /// Gets an optional explicit seed for the new world.
    /// </summary>
    public int? Seed { get; init; }
}
