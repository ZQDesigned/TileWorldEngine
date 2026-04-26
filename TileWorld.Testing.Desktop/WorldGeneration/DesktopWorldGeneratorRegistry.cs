using TileWorld.Engine.World.Generation;

namespace TileWorld.Testing.Desktop.WorldGeneration;

internal static class DesktopWorldGeneratorRegistry
{
    internal static WorldGeneratorRegistry CreateDefault()
    {
        var registry = new WorldGeneratorRegistry();

        var overworldGenerator = new OverworldWorldGenerator();
        registry.Register(overworldGenerator);
        foreach (var alias in DesktopWorldGeneratorIds.OverworldAliases)
        {
            registry.RegisterAlias(alias, overworldGenerator);
        }

        var flatDebugGenerator = new FlatDebugWorldGenerator();
        registry.Register(flatDebugGenerator);
        foreach (var alias in DesktopWorldGeneratorIds.FlatDebugAliases)
        {
            registry.RegisterAlias(alias, flatDebugGenerator);
        }

        var legacyFlatGenerator = new LegacyFlatWorldGenerator();
        registry.Register(legacyFlatGenerator);
        foreach (var alias in DesktopWorldGeneratorIds.LegacyFlatAliases)
        {
            registry.RegisterAlias(alias, legacyFlatGenerator);
        }

        return registry;
    }
}
