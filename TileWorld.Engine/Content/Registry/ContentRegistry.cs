using System;
using System.Collections.Generic;
using TileWorld.Engine.Content.Biomes;
using TileWorld.Engine.Content.Items;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.Content.Tiles;
using TileWorld.Engine.Content.Walls;

namespace TileWorld.Engine.Content.Registry;

/// <summary>
/// Stores tile definitions that are available to the runtime and rendering systems.
/// </summary>
public sealed class ContentRegistry
{
    private readonly Dictionary<int, BiomeDef> _biomeDefs = new();
    private readonly Dictionary<int, ItemDef> _itemDefs = new();
    private readonly Dictionary<int, ObjectDef> _objectDefs = new();
    private readonly Dictionary<ushort, TileDef> _tileDefs = new();
    private readonly Dictionary<ushort, WallDef> _wallDefs = new();

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
            BreakDropItemId = 0,
            AutoTileGroupId = 0
        });
        RegisterWall(new WallDef
        {
            Id = 0,
            Name = "NoWall",
            AutoTileGroupId = 0,
            CountsAsRoomWall = false,
            ObscuresBackground = false,
            BreakDropItemId = 0
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

    /// <summary>
    /// Registers a wall definition by its numeric identifier.
    /// </summary>
    /// <param name="wallDef">The wall definition to register.</param>
    public void RegisterWall(WallDef wallDef)
    {
        ArgumentNullException.ThrowIfNull(wallDef);

        if (_wallDefs.ContainsKey(wallDef.Id))
        {
            throw new InvalidOperationException($"A wall definition with id {wallDef.Id} is already registered.");
        }

        _wallDefs.Add(wallDef.Id, wallDef);
    }

    /// <summary>
    /// Resolves a wall definition or throws when the identifier is unknown.
    /// </summary>
    /// <param name="id">The numeric wall identifier.</param>
    /// <returns>The registered wall definition.</returns>
    public WallDef GetWallDef(ushort id)
    {
        if (!TryGetWallDef(id, out var wallDef))
        {
            throw new KeyNotFoundException($"No wall definition is registered for id {id}.");
        }

        return wallDef;
    }

    /// <summary>
    /// Attempts to resolve a wall definition for the supplied identifier.
    /// </summary>
    /// <param name="id">The numeric wall identifier.</param>
    /// <param name="wallDef">The resolved wall definition when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool TryGetWallDef(ushort id, out WallDef wallDef)
    {
        return _wallDefs.TryGetValue(id, out wallDef!);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the supplied wall identifier is registered.
    /// </summary>
    /// <param name="id">The numeric wall identifier.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool HasWallDef(ushort id)
    {
        return _wallDefs.ContainsKey(id);
    }

    /// <summary>
    /// Enumerates all registered wall definitions.
    /// </summary>
    /// <returns>An enumeration of all registered wall definitions.</returns>
    public IEnumerable<WallDef> EnumerateWallDefs()
    {
        return _wallDefs.Values;
    }

    /// <summary>
    /// Registers a biome definition by its numeric identifier.
    /// </summary>
    /// <param name="biomeDef">The biome definition to register.</param>
    public void RegisterBiome(BiomeDef biomeDef)
    {
        ArgumentNullException.ThrowIfNull(biomeDef);

        if (_biomeDefs.ContainsKey(biomeDef.Id))
        {
            throw new InvalidOperationException($"A biome definition with id {biomeDef.Id} is already registered.");
        }

        _biomeDefs.Add(biomeDef.Id, biomeDef);
    }

    /// <summary>
    /// Resolves a biome definition or throws when the identifier is unknown.
    /// </summary>
    /// <param name="id">The numeric biome identifier.</param>
    /// <returns>The registered biome definition.</returns>
    public BiomeDef GetBiomeDef(int id)
    {
        if (!TryGetBiomeDef(id, out var biomeDef))
        {
            throw new KeyNotFoundException($"No biome definition is registered for id {id}.");
        }

        return biomeDef;
    }

    /// <summary>
    /// Attempts to resolve a biome definition for the supplied identifier.
    /// </summary>
    /// <param name="id">The numeric biome identifier.</param>
    /// <param name="biomeDef">The resolved biome definition when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool TryGetBiomeDef(int id, out BiomeDef biomeDef)
    {
        return _biomeDefs.TryGetValue(id, out biomeDef!);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the supplied biome identifier is registered.
    /// </summary>
    /// <param name="id">The numeric biome identifier.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool HasBiomeDef(int id)
    {
        return _biomeDefs.ContainsKey(id);
    }

    /// <summary>
    /// Enumerates all registered biome definitions.
    /// </summary>
    /// <returns>An enumeration of all registered biome definitions.</returns>
    public IEnumerable<BiomeDef> EnumerateBiomeDefs()
    {
        return _biomeDefs.Values;
    }

    /// <summary>
    /// Registers an object definition by its numeric identifier.
    /// </summary>
    /// <param name="objectDef">The object definition to register.</param>
    public void RegisterObject(ObjectDef objectDef)
    {
        ArgumentNullException.ThrowIfNull(objectDef);

        if (_objectDefs.ContainsKey(objectDef.Id))
        {
            throw new InvalidOperationException($"An object definition with id {objectDef.Id} is already registered.");
        }

        _objectDefs.Add(objectDef.Id, objectDef);
    }

    /// <summary>
    /// Resolves an object definition or throws when the identifier is unknown.
    /// </summary>
    /// <param name="id">The numeric object identifier.</param>
    /// <returns>The registered object definition.</returns>
    public ObjectDef GetObjectDef(int id)
    {
        if (!TryGetObjectDef(id, out var objectDef))
        {
            throw new KeyNotFoundException($"No object definition is registered for id {id}.");
        }

        return objectDef;
    }

    /// <summary>
    /// Attempts to resolve an object definition for the supplied identifier.
    /// </summary>
    /// <param name="id">The numeric object identifier.</param>
    /// <param name="objectDef">The resolved object definition when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool TryGetObjectDef(int id, out ObjectDef objectDef)
    {
        return _objectDefs.TryGetValue(id, out objectDef!);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the supplied object identifier is registered.
    /// </summary>
    /// <param name="id">The numeric object identifier.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool HasObjectDef(int id)
    {
        return _objectDefs.ContainsKey(id);
    }

    /// <summary>
    /// Enumerates all registered object definitions.
    /// </summary>
    /// <returns>An enumeration of all registered object definitions.</returns>
    public IEnumerable<ObjectDef> EnumerateObjectDefs()
    {
        return _objectDefs.Values;
    }

    /// <summary>
    /// Registers an item definition by its numeric identifier.
    /// </summary>
    /// <param name="itemDef">The item definition to register.</param>
    public void RegisterItem(ItemDef itemDef)
    {
        ArgumentNullException.ThrowIfNull(itemDef);

        if (_itemDefs.ContainsKey(itemDef.Id))
        {
            throw new InvalidOperationException($"An item definition with id {itemDef.Id} is already registered.");
        }

        _itemDefs.Add(itemDef.Id, itemDef);
    }

    /// <summary>
    /// Resolves an item definition or throws when the identifier is unknown.
    /// </summary>
    /// <param name="id">The numeric item identifier.</param>
    /// <returns>The registered item definition.</returns>
    public ItemDef GetItemDef(int id)
    {
        if (!TryGetItemDef(id, out var itemDef))
        {
            throw new KeyNotFoundException($"No item definition is registered for id {id}.");
        }

        return itemDef;
    }

    /// <summary>
    /// Attempts to resolve an item definition for the supplied identifier.
    /// </summary>
    /// <param name="id">The numeric item identifier.</param>
    /// <param name="itemDef">The resolved item definition when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool TryGetItemDef(int id, out ItemDef itemDef)
    {
        return _itemDefs.TryGetValue(id, out itemDef!);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the supplied item identifier is registered.
    /// </summary>
    /// <param name="id">The numeric item identifier.</param>
    /// <returns><see langword="true"/> when the identifier is registered.</returns>
    public bool HasItemDef(int id)
    {
        return _itemDefs.ContainsKey(id);
    }

    /// <summary>
    /// Enumerates all registered item definitions.
    /// </summary>
    /// <returns>An enumeration of all registered item definitions.</returns>
    public IEnumerable<ItemDef> EnumerateItemDefs()
    {
        return _itemDefs.Values;
    }
}
