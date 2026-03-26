using System;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World.Generation;

internal sealed class LegacyFlatWorldGenerator : IWorldGenerator
{
    public string GeneratorId => "legacy_flat_v1";

    public int GeneratorVersion => 1;

    public ChunkGenerationResult GenerateChunk(WorldGenerationContext context, ChunkCoord coord)
    {
        return FlatDebugWorldGenerator.GenerateFlatChunk(context, coord, topTileId: 2, subsurfaceTileId: 1, wallId: 1);
    }

    public int GetBiomeId(WorldGenerationContext context, WorldTileCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);
        _ = coord;
        return 1;
    }
}
