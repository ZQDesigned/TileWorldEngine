using System;

namespace TileWorld.Engine.World.Generation;

internal static class WorldGeneratorIdNormalizer
{
    internal const string Overworld = "overworld";
    internal const string FlatDebug = "flat_debug";
    internal const string LegacyFlat = "legacy_flat";

    internal static string Normalize(string generatorId)
    {
        if (string.IsNullOrWhiteSpace(generatorId))
        {
            return Overworld;
        }

        return generatorId.Trim().ToLowerInvariant() switch
        {
            "overworld_v1" => Overworld,
            "flat_debug_v1" => FlatDebug,
            "legacy_flat_v1" => LegacyFlat,
            _ => generatorId.Trim()
        };
    }
}
