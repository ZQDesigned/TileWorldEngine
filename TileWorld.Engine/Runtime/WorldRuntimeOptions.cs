using System;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World.Generation;

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

    /// <summary>
    /// Gets the chunk radius kept active around the current attention center.
    /// </summary>
    public int ActiveRadiusInChunks { get; init; } = 2;

    /// <summary>
    /// Gets the gameplay-provided world generator registry used to resolve metadata generator identifiers.
    /// </summary>
    public WorldGeneratorRegistry WorldGeneratorRegistry { get; init; } = new();

    /// <summary>
    /// Gets the optional fallback generator identifier used when loading legacy worlds with no generator metadata.
    /// </summary>
    public string FallbackGeneratorId { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether runtime-managed liquid simulation is enabled.
    /// </summary>
    public bool EnableLiquidSimulation { get; init; } = true;

    /// <summary>
    /// Gets the maximum number of liquid-dirty chunks simulated per frame.
    /// </summary>
    public int MaxLiquidChunkSimulationsPerFrame { get; init; } = 4;
}
