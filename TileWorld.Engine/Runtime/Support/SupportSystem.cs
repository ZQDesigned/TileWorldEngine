using System.Collections.Generic;
using TileWorld.Engine.Content.Objects;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Support;

/// <summary>
/// Evaluates simple support requirements for placed objects.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="Runtime.WorldRuntime"/> instead of
/// depending on direct support-refresh orchestration.
/// </remarks>
internal sealed class SupportSystem
{
    private readonly Objects.ObjectManager _objectManager;
    private readonly Queries.WorldQueryService _worldQueryService;

    /// <summary>
    /// Creates a support system over the supplied object manager and world query service.
    /// </summary>
    /// <param name="objectManager">The object manager to inspect and mutate.</param>
    /// <param name="worldQueryService">The world query service used for tile support checks.</param>
    public SupportSystem(Objects.ObjectManager objectManager, Queries.WorldQueryService worldQueryService)
    {
        _objectManager = objectManager;
        _worldQueryService = worldQueryService;
    }

    /// <summary>
    /// Returns whether the supplied object footprint currently has required support.
    /// </summary>
    /// <param name="anchorCoord">The logical anchor coordinate of the object.</param>
    /// <param name="objectDef">The object definition to validate.</param>
    /// <returns><see langword="true"/> when support is sufficient.</returns>
    public bool HasSupport(WorldTileCoord anchorCoord, ObjectDef objectDef)
    {
        if (!objectDef.RequiresSupport)
        {
            return true;
        }

        var origin = _objectManager.GetFootprintOrigin(anchorCoord, objectDef);
        for (var x = 0; x < objectDef.SizeInTiles.X; x++)
        {
            var supportCoord = new WorldTileCoord(origin.X + x, origin.Y + objectDef.SizeInTiles.Y);
            if (!_worldQueryService.IsSolid(supportCoord))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Revalidates objects near a changed tile and removes those that lost required support.
    /// </summary>
    /// <param name="coord">The world-tile coordinate that changed.</param>
    public void RefreshAfterTileChanged(WorldTileCoord coord)
    {
        var candidates = new HashSet<int>();

        foreach (var chunkCoord in _objectManager.EnumerateRelevantChunkCoords(coord))
        {
            foreach (var instance in _objectManager.QueryObjectsInChunk(chunkCoord))
            {
                candidates.Add(instance.InstanceId);
            }
        }

        foreach (var instanceId in candidates)
        {
            if (!_objectManager.TryGetObject(instanceId, out var instance) ||
                !_objectManager.TryGetObjectDef(instance.ObjectDefId, out var objectDef) ||
                !objectDef.RequiresSupport)
            {
                continue;
            }

            if (!HasSupport(instance.AnchorCoord, objectDef))
            {
                _objectManager.RemoveObject(instanceId, destroyed: true, spawnDrop: true, publishEvents: true);
            }
        }
    }
}
