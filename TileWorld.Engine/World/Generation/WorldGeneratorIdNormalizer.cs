using System;

namespace TileWorld.Engine.World.Generation;

internal static class WorldGeneratorIdNormalizer
{
    internal static string Normalize(string generatorId)
    {
        if (string.IsNullOrWhiteSpace(generatorId))
        {
            return string.Empty;
        }

        return generatorId.Trim().ToLowerInvariant();
    }
}
