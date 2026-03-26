namespace TileWorld.Engine.Content.Biomes;

/// <summary>
/// Describes a biome profile that world generators can reference when producing terrain and derived biome queries.
/// </summary>
public sealed class BiomeDef
{
    /// <summary>
    /// Gets the numeric biome identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the display name of the biome.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the foreground tile identifier used for the biome's top surface.
    /// </summary>
    public ushort SurfaceTileId { get; init; }

    /// <summary>
    /// Gets the foreground tile identifier used below the surface layer.
    /// </summary>
    public ushort SubsurfaceTileId { get; init; }

    /// <summary>
    /// Gets the background wall identifier used behind biome caves and exposed interior space.
    /// </summary>
    public ushort SurfaceWallId { get; init; }

    /// <summary>
    /// Gets the relative biome priority used when multiple rules overlap.
    /// </summary>
    public byte Priority { get; init; }
}
