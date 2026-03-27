using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World.Generation;

/// <summary>
/// Produces deterministic chunk terrain and biome answers for a persisted world.
/// </summary>
public interface IWorldGenerator
{
    /// <summary>
    /// Gets the stable generator identifier used in world metadata.
    /// </summary>
    string GeneratorId { get; }

    /// <summary>
    /// Gets the version number of the generator implementation.
    /// </summary>
    int GeneratorVersion { get; }

    /// <summary>
    /// Generates terrain for a chunk coordinate.
    /// </summary>
    /// <param name="context">The generation context for the current world.</param>
    /// <param name="coord">The chunk coordinate to generate.</param>
    /// <returns>The generated chunk payload.</returns>
    ChunkGenerationResult GenerateChunk(WorldGenerationContext context, ChunkCoord coord);

    /// <summary>
    /// Resolves the approximate terrain surface height at a world X coordinate.
    /// </summary>
    /// <param name="context">The generation context for the current world.</param>
    /// <param name="worldX">The world X coordinate to inspect.</param>
    /// <returns>The terrain surface tile Y coordinate used by this generator.</returns>
    int GetSurfaceHeight(WorldGenerationContext context, int worldX);

    /// <summary>
    /// Resolves the biome identifier at a world-tile coordinate.
    /// </summary>
    /// <param name="context">The generation context for the current world.</param>
    /// <param name="coord">The world-tile coordinate to inspect.</param>
    /// <returns>The biome identifier at the supplied coordinate.</returns>
    int GetBiomeId(WorldGenerationContext context, WorldTileCoord coord);
}
