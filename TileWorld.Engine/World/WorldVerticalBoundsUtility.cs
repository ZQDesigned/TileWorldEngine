using System;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.World;

/// <summary>
/// Evaluates optional vertical world bounds stored in <see cref="WorldMetadata"/>.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer higher-level runtime helpers instead of
/// depending on raw bounds evaluation rules.
/// </remarks>
internal static class WorldVerticalBoundsUtility
{
    internal static void Validate(WorldMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (metadata.MinTileY is { } minTileY &&
            metadata.MaxTileY is { } maxTileY &&
            minTileY > maxTileY)
        {
            throw new ArgumentException("World vertical bounds are invalid because MinTileY is greater than MaxTileY.", nameof(metadata));
        }
    }

    internal static bool IsTileWithinBounds(WorldMetadata metadata, WorldTileCoord coord)
    {
        return IsTileYWithinBounds(metadata, coord.Y);
    }

    internal static bool IsTileYWithinBounds(WorldMetadata metadata, int tileY)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (metadata.MinTileY is { } minTileY && tileY < minTileY)
        {
            return false;
        }

        if (metadata.MaxTileY is { } maxTileY && tileY > maxTileY)
        {
            return false;
        }

        return true;
    }

    internal static bool DoesChunkIntersectBounds(WorldMetadata metadata, ChunkCoord coord)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(coord);
        var chunkMinTileY = chunkOrigin.Y;
        var chunkMaxTileY = chunkOrigin.Y + ChunkDimensions.Height - 1;

        if (metadata.MinTileY is { } minTileY && chunkMaxTileY < minTileY)
        {
            return false;
        }

        if (metadata.MaxTileY is { } maxTileY && chunkMinTileY > maxTileY)
        {
            return false;
        }

        return true;
    }
}
