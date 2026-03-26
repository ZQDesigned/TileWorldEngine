using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Content.Registry;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Runtime.Events;
using TileWorld.Engine.Runtime.Queries;

namespace TileWorld.Engine.Runtime.Entities;

/// <summary>
/// Manages prototype entities such as the player and collectible drops.
/// </summary>
/// <remarks>
/// This is a gameplay-facing API for prototype sandbox flows. It intentionally remains lightweight and non-ECS so
/// the engine can evolve the entity model in later milestones without introducing backend-specific dependencies.
/// </remarks>
public sealed class EntityManager
{
    private const float PlayerRunSpeedTilesPerSecond = 6f;
    private const float PlayerJumpVelocityTilesPerSecond = -9.5f;
    private const float PlayerGravityTilesPerSecondSquared = 28f;
    private const float DropGravityTilesPerSecondSquared = 24f;
    private const float DropGroundDamping = 0.8f;
    private readonly TileCollisionService _collisionService;
    private readonly ContentRegistry _contentRegistry;
    private readonly Dictionary<int, Entity> _entities = new();
    private readonly WorldEventBus _eventBus;
    private readonly Dictionary<int, PlayerInputState> _playerInputStates = new();
    private int _nextEntityId = 1;

    /// <summary>
    /// Creates an entity manager over the supplied collision and content services.
    /// </summary>
    /// <param name="collisionService">The tile collision service used to resolve entity movement.</param>
    /// <param name="contentRegistry">The content registry used to resolve drop visuals and item identifiers.</param>
    /// <param name="eventBus">The runtime event bus used to publish drop events.</param>
    internal EntityManager(TileCollisionService collisionService, ContentRegistry contentRegistry, WorldEventBus eventBus)
    {
        _collisionService = collisionService;
        _contentRegistry = contentRegistry;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Spawns an entity from an explicit request.
    /// </summary>
    /// <param name="request">The spawn request to realize.</param>
    /// <returns>The created entity identifier.</returns>
    public int Spawn(EntitySpawnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entityId = _nextEntityId++;
        var entity = new Entity
        {
            EntityId = entityId,
            Type = request.Type,
            Position = request.Position,
            Velocity = request.Velocity,
            LocalBounds = request.LocalBounds,
            ItemDefId = request.ItemDefId,
            Amount = Math.Max(1, request.Amount)
        };

        _entities.Add(entityId, entity);
        if (entity.Type == EntityType.Drop)
        {
            _eventBus.Publish(new DropSpawnedEvent(entity.EntityId, entity.ItemDefId, entity.Position, entity.Amount));
        }

        return entityId;
    }

    /// <summary>
    /// Spawns a controllable player prototype at the supplied position.
    /// </summary>
    /// <param name="position">The starting position in world tile units.</param>
    /// <returns>The created player entity identifier.</returns>
    public int SpawnPlayer(Float2 position)
    {
        return Spawn(new EntitySpawnRequest
        {
            Type = EntityType.Player,
            Position = position,
            LocalBounds = new AabbF(0.05f, 0.05f, 0.9f, 1.9f)
        });
    }

    /// <summary>
    /// Spawns a collectible world drop at the supplied position.
    /// </summary>
    /// <param name="itemDefId">The dropped item definition identifier.</param>
    /// <param name="position">The spawn position in world tile units.</param>
    /// <param name="amount">The dropped amount.</param>
    /// <returns>The created drop entity identifier, or <c>0</c> when the item definition is invalid.</returns>
    public int SpawnDrop(int itemDefId, Float2 position, int amount = 1)
    {
        if (!_contentRegistry.HasItemDef(itemDefId))
        {
            return 0;
        }

        return Spawn(new EntitySpawnRequest
        {
            Type = EntityType.Drop,
            Position = position,
            LocalBounds = new AabbF(0.15f, 0.15f, 0.7f, 0.7f),
            ItemDefId = itemDefId,
            Amount = amount
        });
    }

    /// <summary>
    /// Attempts to resolve an entity by identifier.
    /// </summary>
    /// <param name="entityId">The entity identifier to resolve.</param>
    /// <param name="entity">The resolved entity when present.</param>
    /// <returns><see langword="true"/> when the entity exists.</returns>
    public bool TryGetEntity(int entityId, out Entity entity)
    {
        return _entities.TryGetValue(entityId, out entity!);
    }

    /// <summary>
    /// Enumerates all currently active entities.
    /// </summary>
    /// <returns>The active entities.</returns>
    public IEnumerable<Entity> EnumerateEntities()
    {
        return _entities.Values;
    }

    /// <summary>
    /// Applies player control input that should be consumed during the next update.
    /// </summary>
    /// <param name="entityId">The target player entity identifier.</param>
    /// <param name="moveAxis">The horizontal movement axis in the range [-1, 1].</param>
    /// <param name="jumpRequested">Whether a jump should be attempted during the next update.</param>
    public void SetPlayerInput(int entityId, float moveAxis, bool jumpRequested)
    {
        _playerInputStates[entityId] = new PlayerInputState(Math.Clamp(moveAxis, -1f, 1f), jumpRequested);
    }

    /// <summary>
    /// Advances entity simulation for one frame.
    /// </summary>
    /// <param name="frameTime">The current frame timing snapshot.</param>
    public void Update(FrameTime frameTime)
    {
        var deltaSeconds = (float)frameTime.ElapsedTime.TotalSeconds;
        if (deltaSeconds <= 0f)
        {
            return;
        }

        foreach (var entity in _entities.Values)
        {
            switch (entity.Type)
            {
                case EntityType.Player:
                    UpdatePlayer(entity, deltaSeconds);
                    break;
                case EntityType.Drop:
                    UpdateDrop(entity, deltaSeconds);
                    break;
            }
        }

        ResolveDropCollection();
        RemovePendingEntities();
    }

    /// <summary>
    /// Marks an entity for removal.
    /// </summary>
    /// <param name="entityId">The entity identifier to remove.</param>
    /// <returns><see langword="true"/> when the entity existed.</returns>
    public bool RemoveEntity(int entityId)
    {
        if (!_entities.TryGetValue(entityId, out var entity))
        {
            return false;
        }

        entity.StateFlags |= EntityStateFlags.PendingRemoval;
        return true;
    }

    private void UpdatePlayer(Entity entity, float deltaSeconds)
    {
        _playerInputStates.TryGetValue(entity.EntityId, out var inputState);
        entity.Velocity = entity.Velocity with { X = inputState.MoveAxis * PlayerRunSpeedTilesPerSecond };

        if (inputState.JumpRequested && (entity.StateFlags & EntityStateFlags.Grounded) != 0)
        {
            entity.Velocity = entity.Velocity with { Y = PlayerJumpVelocityTilesPerSecond };
            entity.StateFlags &= ~EntityStateFlags.Grounded;
        }

        entity.Velocity = entity.Velocity with { Y = entity.Velocity.Y + (PlayerGravityTilesPerSecondSquared * deltaSeconds) };
        _collisionService.MoveAndCollide(entity, entity.Velocity * deltaSeconds);
        _playerInputStates[entity.EntityId] = inputState with { JumpRequested = false };
    }

    private void UpdateDrop(Entity entity, float deltaSeconds)
    {
        entity.Velocity = entity.Velocity with { Y = entity.Velocity.Y + (DropGravityTilesPerSecondSquared * deltaSeconds) };
        _collisionService.MoveAndCollide(entity, entity.Velocity * deltaSeconds);

        if ((entity.StateFlags & EntityStateFlags.Grounded) != 0)
        {
            entity.Velocity = entity.Velocity with
            {
                X = entity.Velocity.X * DropGroundDamping,
                Y = 0f
            };
        }
    }

    private void ResolveDropCollection()
    {
        var players = _entities.Values.Where(entity => entity.Type == EntityType.Player).ToArray();
        if (players.Length == 0)
        {
            return;
        }

        foreach (var drop in _entities.Values.Where(entity => entity.Type == EntityType.Drop).ToArray())
        {
            foreach (var player in players)
            {
                if (!drop.WorldBounds.Intersects(player.WorldBounds))
                {
                    continue;
                }

                drop.StateFlags |= EntityStateFlags.PendingRemoval;
                _eventBus.Publish(new DropCollectedEvent(drop.EntityId, drop.ItemDefId, player.EntityId, drop.Amount));
                break;
            }
        }
    }

    private void RemovePendingEntities()
    {
        var removedIds = _entities.Values
            .Where(entity => (entity.StateFlags & EntityStateFlags.PendingRemoval) != 0)
            .Select(entity => entity.EntityId)
            .ToArray();

        foreach (var entityId in removedIds)
        {
            _entities.Remove(entityId);
            _playerInputStates.Remove(entityId);
        }
    }

    private readonly record struct PlayerInputState(float MoveAxis, bool JumpRequested);
}
