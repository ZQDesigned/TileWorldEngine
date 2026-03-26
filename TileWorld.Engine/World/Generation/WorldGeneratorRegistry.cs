using System;
using System.Collections.Generic;

namespace TileWorld.Engine.World.Generation;

/// <summary>
/// Resolves built-in world generators by their stable metadata identifiers.
/// </summary>
internal sealed class WorldGeneratorRegistry
{
    private readonly Dictionary<string, IWorldGenerator> _generators;

    private WorldGeneratorRegistry(IEnumerable<IWorldGenerator> generators)
    {
        _generators = new Dictionary<string, IWorldGenerator>(StringComparer.OrdinalIgnoreCase);

        foreach (var generator in generators)
        {
            _generators[generator.GeneratorId] = generator;
        }
    }

    internal static WorldGeneratorRegistry CreateDefault()
    {
        return new WorldGeneratorRegistry(
        [
            new OverworldWorldGenerator(),
            new FlatDebugWorldGenerator(),
            new LegacyFlatWorldGenerator()
        ]);
    }

    internal IWorldGenerator ResolveOrDefault(string generatorId)
    {
        if (!string.IsNullOrWhiteSpace(generatorId) &&
            _generators.TryGetValue(generatorId, out var generator))
        {
            return generator;
        }

        return _generators["overworld_v1"];
    }
}
