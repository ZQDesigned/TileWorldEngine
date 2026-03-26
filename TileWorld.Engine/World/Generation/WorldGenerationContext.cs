using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.World;

namespace TileWorld.Engine.World.Generation;

/// <summary>
/// Provides immutable world context to a generator while it resolves terrain and biome data.
/// </summary>
public sealed class WorldGenerationContext
{
    /// <summary>
    /// Gets the persistent metadata associated with the world being generated.
    /// </summary>
    public required WorldMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the content registry used to resolve biome-driven tile and wall definitions.
    /// </summary>
    public required ContentRegistry ContentRegistry { get; init; }
}
