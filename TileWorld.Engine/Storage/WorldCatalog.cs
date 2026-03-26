using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.World;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Scans and creates persisted worlds inside a common worlds root directory.
/// </summary>
public sealed class WorldCatalog
{
    private readonly WorldStorage _worldStorage;

    /// <summary>
    /// Creates a world catalog rooted at the default application worlds directory.
    /// </summary>
    public WorldCatalog()
        : this(Path.Combine(AppContext.BaseDirectory, "Worlds"), new WorldStorage())
    {
    }

    /// <summary>
    /// Creates a world catalog with explicit root path and storage dependencies.
    /// </summary>
    /// <param name="worldsRootPath">The directory that contains all world folders.</param>
    /// <param name="worldStorage">The storage backend used to load and save world metadata.</param>
    public WorldCatalog(string worldsRootPath, WorldStorage worldStorage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worldsRootPath);
        ArgumentNullException.ThrowIfNull(worldStorage);

        WorldsRootPath = worldsRootPath;
        _worldStorage = worldStorage;
    }

    /// <summary>
    /// Gets the root directory that contains all persisted worlds.
    /// </summary>
    public string WorldsRootPath { get; }

    /// <summary>
    /// Enumerates all valid worlds currently persisted under the catalog root.
    /// </summary>
    /// <returns>The discovered world entries sorted by most recent activity.</returns>
    public IReadOnlyList<WorldCatalogEntry> EnumerateWorlds()
    {
        if (!Directory.Exists(WorldsRootPath))
        {
            return [];
        }

        var entries = new List<WorldCatalogEntry>();

        foreach (var directoryPath in Directory.EnumerateDirectories(WorldsRootPath))
        {
            var metadataPath = Path.Combine(directoryPath, "world.json");
            if (!File.Exists(metadataPath))
            {
                EngineDiagnostics.Warn($"WorldCatalog skipped '{directoryPath}' because world.json is missing.");
                continue;
            }

            try
            {
                var metadata = _worldStorage.LoadMetadata(directoryPath);
                var hasChunkData = Directory.Exists(Path.Combine(directoryPath, "chunks")) &&
                                   Directory.EnumerateFiles(Path.Combine(directoryPath, "chunks"), "*.chk", SearchOption.TopDirectoryOnly).Any();
                entries.Add(new WorldCatalogEntry
                {
                    WorldPath = directoryPath,
                    DirectoryName = Path.GetFileName(directoryPath),
                    WorldId = metadata.WorldId,
                    Name = metadata.Name,
                    LastWriteTimeUtc = GetLastWriteTimeUtc(directoryPath, metadataPath),
                    HasChunkData = hasChunkData
                });
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                EngineDiagnostics.Warn($"WorldCatalog skipped invalid world directory '{directoryPath}': {exception.Message}");
            }
        }

        return entries
            .OrderByDescending(entry => entry.LastWriteTimeUtc)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Creates a new world folder with minimal metadata and returns the resulting catalog entry.
    /// </summary>
    /// <param name="options">The creation parameters for the new world.</param>
    /// <returns>The newly created world catalog entry.</returns>
    public WorldCatalogEntry CreateWorld(WorldCreationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var name = (options.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("World name cannot be empty.", nameof(options));
        }

        if (options.MinTileY is { } minTileY &&
            options.MaxTileY is { } maxTileY &&
            minTileY > maxTileY)
        {
            throw new ArgumentException("World vertical bounds are invalid because MinTileY is greater than MaxTileY.", nameof(options));
        }

        Directory.CreateDirectory(WorldsRootPath);

        var directoryName = CreateUniqueDirectoryName(name);
        var worldPath = Path.Combine(WorldsRootPath, directoryName);
        var metadata = new WorldMetadata
        {
            WorldId = Guid.NewGuid().ToString("N"),
            Name = name,
            Seed = options.Seed ?? Random.Shared.Next(int.MinValue, int.MaxValue),
            GeneratorId = string.IsNullOrWhiteSpace(options.GeneratorId) ? "overworld_v1" : options.GeneratorId,
            GeneratorVersion = 1,
            SpawnTile = options.SpawnTile,
            MinTileY = options.MinTileY,
            MaxTileY = options.MaxTileY
        };

        _worldStorage.SaveMetadata(worldPath, metadata);

        return new WorldCatalogEntry
        {
            WorldPath = worldPath,
            DirectoryName = directoryName,
            WorldId = metadata.WorldId,
            Name = metadata.Name,
            LastWriteTimeUtc = File.GetLastWriteTimeUtc(Path.Combine(worldPath, "world.json")),
            HasChunkData = false
        };
    }

    private string CreateUniqueDirectoryName(string worldName)
    {
        var baseName = SanitizeDirectoryName(worldName);
        var candidate = baseName;
        var suffix = 2;

        while (Directory.Exists(Path.Combine(WorldsRootPath, candidate)))
        {
            candidate = $"{baseName}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static DateTime GetLastWriteTimeUtc(string worldPath, string metadataPath)
    {
        var lastWriteTimeUtc = File.GetLastWriteTimeUtc(metadataPath);
        var chunksDirectory = Path.Combine(worldPath, "chunks");
        if (!Directory.Exists(chunksDirectory))
        {
            return lastWriteTimeUtc;
        }

        foreach (var chunkPath in Directory.EnumerateFiles(chunksDirectory, "*.chk", SearchOption.TopDirectoryOnly))
        {
            var chunkWriteTime = File.GetLastWriteTimeUtc(chunkPath);
            if (chunkWriteTime > lastWriteTimeUtc)
            {
                lastWriteTimeUtc = chunkWriteTime;
            }
        }

        return lastWriteTimeUtc;
    }

    private static string SanitizeDirectoryName(string worldName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitizedCharacters = worldName
            .Trim()
            .Select(character =>
            {
                if (invalidCharacters.Contains(character))
                {
                    return '-';
                }

                return char.IsWhiteSpace(character) ? '-' : character;
            })
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            .ToArray();

        var sanitized = new string(sanitizedCharacters).Trim('-');
        return string.IsNullOrWhiteSpace(sanitized)
            ? "world"
            : sanitized;
    }
}
