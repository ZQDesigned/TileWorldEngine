using System;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Entities;

/// <summary>
/// Performs simple axis-aligned collision resolution between entities and world movement blockers.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="Runtime.WorldRuntime"/> and let
/// the runtime coordinate entity movement against world geometry.
/// </remarks>
internal sealed class TileCollisionService
{
    private const float Epsilon = 0.0001f;
    private readonly WorldQueryService _worldQueryService;

    /// <summary>
    /// Creates a tile-collision service over the supplied world query service.
    /// </summary>
    /// <param name="worldQueryService">The world query service used to inspect solid tiles.</param>
    public TileCollisionService(WorldQueryService worldQueryService)
    {
        _worldQueryService = worldQueryService;
    }

    /// <summary>
    /// Gets the underlying world query service used by collision resolution.
    /// </summary>
    internal WorldQueryService QueryService => _worldQueryService;

    /// <summary>
    /// Moves an entity by a proposed delta while resolving collisions against blocking foreground tiles and objects.
    /// </summary>
    /// <param name="entity">The entity to move.</param>
    /// <param name="delta">The proposed movement delta in tile units.</param>
    public void MoveAndCollide(Entity entity, Float2 delta)
    {
        entity.StateFlags &= ~EntityStateFlags.Grounded;

        if (delta.X != 0f)
        {
            MoveHorizontal(entity, delta.X);
        }

        if (delta.Y != 0f)
        {
            MoveVertical(entity, delta.Y);
        }
    }

    private void MoveHorizontal(Entity entity, float deltaX)
    {
        var nextPosition = entity.Position with { X = entity.Position.X + deltaX };
        var nextBounds = entity.LocalBounds.Translate(nextPosition);

        if (deltaX > 0f)
        {
            var tileX = FloorToInt(nextBounds.Right - Epsilon);
            var minTileY = FloorToInt(nextBounds.Top + Epsilon);
            var maxTileY = FloorToInt(nextBounds.Bottom - Epsilon);

            for (var tileY = minTileY; tileY <= maxTileY; tileY++)
            {
                if (!_worldQueryService.IsMovementBlocked(new WorldTileCoord(tileX, tileY), entity.Type))
                {
                    continue;
                }

                var resolvedX = tileX - entity.LocalBounds.Right;
                entity.Position = entity.Position with { X = resolvedX };
                entity.Velocity = entity.Velocity with { X = 0f };
                return;
            }
        }
        else if (deltaX < 0f)
        {
            var tileX = FloorToInt(nextBounds.Left + Epsilon);
            var minTileY = FloorToInt(nextBounds.Top + Epsilon);
            var maxTileY = FloorToInt(nextBounds.Bottom - Epsilon);

            for (var tileY = minTileY; tileY <= maxTileY; tileY++)
            {
                if (!_worldQueryService.IsMovementBlocked(new WorldTileCoord(tileX, tileY), entity.Type))
                {
                    continue;
                }

                var resolvedX = tileX + 1f - entity.LocalBounds.Left;
                entity.Position = entity.Position with { X = resolvedX };
                entity.Velocity = entity.Velocity with { X = 0f };
                return;
            }
        }

        entity.Position = nextPosition;
    }

    private void MoveVertical(Entity entity, float deltaY)
    {
        var nextPosition = entity.Position with { Y = entity.Position.Y + deltaY };
        var nextBounds = entity.LocalBounds.Translate(nextPosition);
        if (deltaY < 0f &&
            _worldQueryService.Metadata.MinTileY is { } minTileY &&
            nextBounds.Top < minTileY)
        {
            entity.Position = entity.Position with { Y = minTileY - entity.LocalBounds.Top };
            entity.Velocity = entity.Velocity with { Y = 0f };
            return;
        }

        if (deltaY > 0f &&
            _worldQueryService.Metadata.MaxTileY is { } maxTileY &&
            nextBounds.Bottom > maxTileY + 1f)
        {
            entity.Position = entity.Position with { Y = (maxTileY + 1f) - entity.LocalBounds.Bottom };
            entity.Velocity = entity.Velocity with { Y = 0f };
            entity.StateFlags |= EntityStateFlags.Grounded;
            return;
        }

        if (deltaY > 0f)
        {
            var tileY = FloorToInt(nextBounds.Bottom - Epsilon);
            var minTileX = FloorToInt(nextBounds.Left + Epsilon);
            var maxTileX = FloorToInt(nextBounds.Right - Epsilon);

            for (var tileX = minTileX; tileX <= maxTileX; tileX++)
            {
                if (!_worldQueryService.IsMovementBlocked(new WorldTileCoord(tileX, tileY), entity.Type))
                {
                    continue;
                }

                var resolvedY = tileY - entity.LocalBounds.Bottom;
                entity.Position = entity.Position with { Y = resolvedY };
                entity.Velocity = entity.Velocity with { Y = 0f };
                entity.StateFlags |= EntityStateFlags.Grounded;
                return;
            }
        }
        else if (deltaY < 0f)
        {
            var tileY = FloorToInt(nextBounds.Top + Epsilon);
            var minTileX = FloorToInt(nextBounds.Left + Epsilon);
            var maxTileX = FloorToInt(nextBounds.Right - Epsilon);

            for (var tileX = minTileX; tileX <= maxTileX; tileX++)
            {
                if (!_worldQueryService.IsMovementBlocked(new WorldTileCoord(tileX, tileY), entity.Type))
                {
                    continue;
                }

                var resolvedY = tileY + 1f - entity.LocalBounds.Top;
                entity.Position = entity.Position with { Y = resolvedY };
                entity.Velocity = entity.Velocity with { Y = 0f };
                return;
            }
        }

        entity.Position = nextPosition;
    }

    private static int FloorToInt(float value)
    {
        return (int)MathF.Floor(value);
    }
}
