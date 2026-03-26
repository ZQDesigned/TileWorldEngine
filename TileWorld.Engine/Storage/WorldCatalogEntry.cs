using System;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Describes a persisted world discovered by <see cref="WorldCatalog"/>.
/// </summary>
public sealed class WorldCatalogEntry
{
    /// <summary>
    /// Gets the absolute path to the world root directory.
    /// </summary>
    public string WorldPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the world directory name relative to the catalog root.
    /// </summary>
    public string DirectoryName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the stable world identifier stored in metadata.
    /// </summary>
    public string WorldId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name stored in metadata.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the most recent write time observed within the world directory.
    /// </summary>
    public DateTime LastWriteTimeUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether at least one chunk payload exists on disk.
    /// </summary>
    public bool HasChunkData { get; init; }
}
