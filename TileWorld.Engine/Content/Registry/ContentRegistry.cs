using System;
using System.Collections.Generic;
using TileWorld.Engine.Content.Tiles;

namespace TileWorld.Engine.Content.Registry;

/// <summary>
/// Stores tile definitions that are available to the runtime and rendering systems.
/// </summary>
public sealed class ContentRegistry
{
    private readonly Dictionary<ushort, TileDef> _tileDefs = new();

    /// <summary>
    /// Creates a registry and seeds it with the built-in air tile definition.
    /// </summary>
    public ContentRegistry()
    {
        RegisterTile(new TileDef
        {
            Id = 0,
            Name = "Air",
            Category = "Base",
            IsSolid = false,
            BlocksLight = false,
            CanBeMined = false,
            Hardness = 0,
            AutoTileGroupId = 0
        });
    }

    /// <summary>
    /// Registers a tile definition by its numeric identifier.
    /// </summary>
    /// <param name="tileDef">The tile definition to register.</param>
    public void RegisterTile(TileDef tileDef)
    {
        ArgumentNullException.ThrowIfNull(tileDef);

        if (_tileDefs.ContainsKey(tileDef.Id))
        {
            throw new InvalidOperationException($"A tile definition with id {tileDef.Id} is already registered.");
        }

        _tileDefs.Add(tileDef.Id, tileDef);
    }

    /// <summary>
    /// Resolves a tile definition or throws when the identifier is unknown.
    /// </summary>
    /// <param name="id">The numeric tile identifier.</param>
    /// <returns>The registered tile definition.</returns>
    public TileDef GetTileDef(ushort id)
    {
        if (!TryGetTileDef(id, out var tileDef))
        {
            throw new KeyNotFoundException($"No tile definition is registered for id {id}.");
        }

        return tileDef;
    }

    /// <summary>
    /// Attempts to resolve a tile definition for the supplied identifier.
    /// </summary>
    /// <param name="id">The numeric tile identifier.</param>
    /// <param name="tileDef">The resolved tile definition when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool TryGetTileDef(ushort id, out TileDef tileDef)
    {
        return _tileDefs.TryGetValue(id, out tileDef!);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the identifier is registered.
    /// </summary>
    /// <param name="id">The numeric tile identifier.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool HasTileDef(ushort id)
    {
        return _tileDefs.ContainsKey(id);
    }

    /// <summary>
    /// Enumerates all registered tile definitions.
    /// </summary>
    /// <returns>An enumeration of all registered tile definitions.</returns>
    public IEnumerable<TileDef> EnumerateTileDefs()
    {
        return _tileDefs.Values;
    }
}
