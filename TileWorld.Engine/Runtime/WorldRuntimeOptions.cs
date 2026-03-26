using System;
using TileWorld.Engine.Storage;

namespace TileWorld.Engine.Runtime;

/// <summary>
/// Configures persistence and automatic save behavior for a world runtime.
/// </summary>
public sealed class WorldRuntimeOptions
{
    /// <summary>
    /// Gets the root directory path of the world on disk. Leave empty to disable persistence.
    /// </summary>
    public string WorldPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the storage backend used for metadata and chunk payloads when persistence is enabled.
    /// </summary>
    public WorldStorage WorldStorage { get; init; } = null!;

    /// <summary>
    /// Gets a value indicating whether <see cref="WorldRuntime.Shutdown"/> should trigger a final save.
    /// </summary>
    public bool SaveOnShutdown { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether runtime-managed automatic saving is enabled.
    /// </summary>
    public bool EnableAutoSave { get; init; } = true;

    /// <summary>
    /// Gets the maximum time allowed between automatic save attempts while dirty save data exists.
    /// </summary>
    public TimeSpan AutoSaveInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the idle time required after the last observed mutation before an automatic save may trigger.
    /// </summary>
    public TimeSpan AutoSaveIdleDelay { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Gets the minimum spacing enforced between consecutive automatic saves.
    /// </summary>
    public TimeSpan MinimumAutoSaveSpacing { get; init; } = TimeSpan.FromSeconds(5);
}
