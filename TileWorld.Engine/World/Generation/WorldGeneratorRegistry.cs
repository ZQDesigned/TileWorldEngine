using System;
using System.Collections.Generic;

namespace TileWorld.Engine.World.Generation;

/// <summary>
/// Stores gameplay-provided world generators and resolves them by stable metadata identifier.
/// </summary>
/// <remarks>
/// The engine does not ship with a concrete terrain algorithm. Games and test hosts are expected to register their
/// own generator implementations and pass the registry into <see cref="Runtime.WorldRuntimeOptions"/>.
/// </remarks>
public sealed class WorldGeneratorRegistry
{
    private readonly Dictionary<string, IWorldGenerator> _generators = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a concrete world generator under its declared metadata identifier.
    /// </summary>
    /// <param name="generator">The generator implementation to register.</param>
    public void Register(IWorldGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);

        var normalizedGeneratorId = WorldGeneratorIdNormalizer.Normalize(generator.GeneratorId);
        if (string.IsNullOrWhiteSpace(normalizedGeneratorId))
        {
            throw new ArgumentException("World generators must declare a non-empty generator identifier.", nameof(generator));
        }

        _generators[normalizedGeneratorId] = generator;
    }

    /// <summary>
    /// Registers an alias that resolves to an already-constructed generator.
    /// </summary>
    /// <param name="alias">The alternate metadata identifier that should resolve to the generator.</param>
    /// <param name="generator">The generator implementation that should be returned for the alias.</param>
    public void RegisterAlias(string alias, IWorldGenerator generator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentNullException.ThrowIfNull(generator);

        var normalizedAlias = WorldGeneratorIdNormalizer.Normalize(alias);
        if (string.IsNullOrWhiteSpace(normalizedAlias))
        {
            throw new ArgumentException("Generator aliases must normalize to a non-empty identifier.", nameof(alias));
        }

        _generators[normalizedAlias] = generator;
    }

    /// <summary>
    /// Attempts to resolve a registered generator by metadata identifier.
    /// </summary>
    /// <param name="generatorId">The metadata identifier to resolve.</param>
    /// <param name="generator">The resolved generator when registration exists.</param>
    /// <returns><see langword="true"/> when a registration exists for the supplied identifier.</returns>
    public bool TryResolve(string generatorId, out IWorldGenerator generator)
    {
        return _generators.TryGetValue(WorldGeneratorIdNormalizer.Normalize(generatorId), out generator!);
    }

    /// <summary>
    /// Resolves a registered generator or returns the provided fallback implementation.
    /// </summary>
    /// <param name="generatorId">The metadata identifier to resolve.</param>
    /// <param name="fallbackGenerator">The fallback generator that should be returned when no registration exists.</param>
    /// <returns>The resolved generator when present; otherwise the supplied fallback generator.</returns>
    public IWorldGenerator ResolveOrDefault(string generatorId, IWorldGenerator fallbackGenerator)
    {
        ArgumentNullException.ThrowIfNull(fallbackGenerator);

        return TryResolve(generatorId, out var resolvedGenerator)
            ? resolvedGenerator
            : fallbackGenerator;
    }
}
