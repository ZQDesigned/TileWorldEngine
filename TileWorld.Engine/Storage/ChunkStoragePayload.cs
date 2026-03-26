using System.Collections.Generic;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Objects;

namespace TileWorld.Engine.Storage;

/// <summary>
/// Represents the persisted payload of a chunk, including chunk cell data and anchored object instances.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="WorldStorage"/> methods instead of
/// depending on raw chunk storage payloads.
/// </remarks>
internal sealed class ChunkStoragePayload
{
    /// <summary>
    /// Creates a chunk storage payload.
    /// </summary>
    /// <param name="chunk">The deserialized chunk cell data.</param>
    /// <param name="anchoredObjects">The object instances anchored in this chunk.</param>
    public ChunkStoragePayload(Chunk chunk, IReadOnlyList<ObjectInstance> anchoredObjects)
    {
        Chunk = chunk;
        AnchoredObjects = anchoredObjects;
    }

    /// <summary>
    /// Gets the chunk cell data.
    /// </summary>
    public Chunk Chunk { get; }

    /// <summary>
    /// Gets the object instances anchored in this chunk.
    /// </summary>
    public IReadOnlyList<ObjectInstance> AnchoredObjects { get; }
}
