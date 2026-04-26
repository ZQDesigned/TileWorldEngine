using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World.Generation;

internal sealed class EmptyWorldGenerator : IWorldGenerator
{
    internal static EmptyWorldGenerator Instance { get; } = new();

    public string GeneratorId => string.Empty;

    public int GeneratorVersion => 1;

    public ChunkGenerationResult GenerateChunk(WorldGenerationContext context, ChunkCoord coord)
    {
        return new ChunkGenerationResult(new Chunk(coord));
    }

    public int GetSurfaceHeight(WorldGenerationContext context, int worldX)
    {
        _ = context;
        _ = worldX;
        return 0;
    }

    public int GetBiomeId(WorldGenerationContext context, WorldTileCoord coord)
    {
        _ = context;
        _ = coord;
        return 0;
    }
}
