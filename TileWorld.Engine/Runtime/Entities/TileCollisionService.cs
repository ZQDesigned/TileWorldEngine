using System;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Entities;

/// <summary>
/// Performs simple axis-aligned collision resolution between entities and solid tiles.
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
    /// Moves an entity by a proposed delta while resolving collisions against solid foreground tiles.
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
                if (!_worldQueryService.IsSolid(new WorldTileCoord(tileX, tileY)))
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
                if (!_worldQueryService.IsSolid(new WorldTileCoord(tileX, tileY)))
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

        if (deltaY > 0f)
        {
            var tileY = FloorToInt(nextBounds.Bottom - Epsilon);
            var minTileX = FloorToInt(nextBounds.Left + Epsilon);
            var maxTileX = FloorToInt(nextBounds.Right - Epsilon);

            for (var tileX = minTileX; tileX <= maxTileX; tileX++)
            {
                if (!_worldQueryService.IsSolid(new WorldTileCoord(tileX, tileY)))
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
                if (!_worldQueryService.IsSolid(new WorldTileCoord(tileX, tileY)))
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
