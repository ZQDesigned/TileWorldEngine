using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Runtime.Contexts;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Tests.Runtime.Queries;

public sealed class WorldQueryServiceInactiveChunkTests
{
    [Fact]
    public void InactiveChunks_RequireAllowInactiveChunkOption()
    {
        var worldData = new WorldData(new WorldMetadata());
        var chunk = worldData.GetOrCreateChunk(new ChunkCoord(0, 0));
        chunk.State = ChunkState.Inactive;
        chunk.SetCell(0, 0, new() { ForegroundTileId = 1 });
        var queryService = new WorldQueryService(worldData, new ContentRegistry());

        var withoutInactive = queryService.GetCell(new WorldTileCoord(0, 0));
        var withInactive = queryService.GetCell(new WorldTileCoord(0, 0), new QueryOptions { AllowInactiveChunk = true });

        Assert.Equal((ushort)0, withoutInactive.ForegroundTileId);
        Assert.Equal((ushort)1, withInactive.ForegroundTileId);
    }
}
