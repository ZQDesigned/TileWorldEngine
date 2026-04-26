using System;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;
using TileWorld.Engine.World.Generation;

namespace TileWorld.Testing.Desktop.WorldGeneration;

internal sealed class LegacyFlatWorldGenerator : IWorldGenerator
{
    public string GeneratorId => DesktopWorldGeneratorIds.LegacyFlat;

    public int GeneratorVersion => 1;

    public ChunkGenerationResult GenerateChunk(WorldGenerationContext context, ChunkCoord coord)
    {
        return FlatDebugWorldGenerator.GenerateFlatChunk(context, coord, topTileId: 2, subsurfaceTileId: 1, wallId: 1);
    }

    public int GetSurfaceHeight(WorldGenerationContext context, int worldX)
    {
        ArgumentNullException.ThrowIfNull(context);
        _ = worldX;
        return context.Metadata.SpawnTile.Y + 2;
    }

    public int GetBiomeId(WorldGenerationContext context, WorldTileCoord coord)
    {
        ArgumentNullException.ThrowIfNull(context);
        _ = coord;
        return 1;
    }
}
