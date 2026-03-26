using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.World;

/// <summary>
/// Stores persistent metadata for a world.
/// </summary>
public sealed class WorldMetadata
{
    /// <summary>
    /// Gets the stable identifier used to associate persisted data with this world.
    /// </summary>
    public string WorldId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name of the world.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the seed value associated with world generation.
    /// </summary>
    public int Seed { get; init; }

    /// <summary>
    /// Gets the version number of the world metadata format.
    /// </summary>
    public int WorldFormatVersion { get; init; } = 2;

    /// <summary>
    /// Gets the version number of the chunk payload format expected by this world.
    /// </summary>
    public int ChunkFormatVersion { get; init; } = 2;

    /// <summary>
    /// Gets the generator identifier associated with this world's terrain layout.
    /// </summary>
    public string GeneratorId { get; init; } = "overworld_v1";

    /// <summary>
    /// Gets the version number of the generator implementation associated with this world.
    /// </summary>
    public int GeneratorVersion { get; init; } = 1;

    /// <summary>
    /// Gets the persisted world time counter.
    /// </summary>
    public long WorldTime { get; init; }

    /// <summary>
    /// Gets the world bounds interpretation mode used by higher-level systems.
    /// </summary>
    public WorldBoundsMode BoundsMode { get; init; } = WorldBoundsMode.LargeFinite;

    /// <summary>
    /// Gets the default spawn tile coordinate for new players or tools.
    /// </summary>
    public Int2 SpawnTile { get; init; } = Int2.Zero;

    /// <summary>
    /// Gets the chunk width that persisted content was authored against.
    /// </summary>
    public int ChunkWidth { get; init; } = World.Chunks.ChunkDimensions.Width;

    /// <summary>
    /// Gets the chunk height that persisted content was authored against.
    /// </summary>
    public int ChunkHeight { get; init; } = World.Chunks.ChunkDimensions.Height;
}
