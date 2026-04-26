using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Entities;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World.Cells;

namespace TileWorld.Engine.Tests.Storage;

public sealed class RuntimeEntityBinarySerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTripsRuntimeEntityData()
    {
        var serializer = new RuntimeEntityBinarySerializer();
        var entities = new[]
        {
            new Entity
            {
                EntityId = 9,
                Type = EntityType.Drop,
                Position = new Float2(8.5f, 4.25f),
                Velocity = new Float2(-0.5f, 1.75f),
                LocalBounds = new AabbF(0.15f, 0.15f, 0.7f, 0.7f),
                StateFlags = EntityStateFlags.Grounded,
                ItemDefId = 1001,
                Amount = 4
            }
        };

        var payload = serializer.Serialize(entities);
        var restored = serializer.Deserialize(payload);

        var restoredEntity = Assert.Single(restored);
        Assert.Equal(entities[0].EntityId, restoredEntity.EntityId);
        Assert.Equal(entities[0].Type, restoredEntity.Type);
        Assert.Equal(entities[0].Position, restoredEntity.Position);
        Assert.Equal(entities[0].Velocity, restoredEntity.Velocity);
        Assert.Equal(entities[0].LocalBounds, restoredEntity.LocalBounds);
        Assert.Equal(entities[0].StateFlags, restoredEntity.StateFlags);
        Assert.Equal(entities[0].ItemDefId, restoredEntity.ItemDefId);
        Assert.Equal(entities[0].Amount, restoredEntity.Amount);
    }

    [Fact]
    public void Deserialize_WhenMagicIsInvalid_ThrowsInvalidDataException()
    {
        var serializer = new RuntimeEntityBinarySerializer();
        var invalidPayload = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        Assert.Throws<InvalidDataException>(() => serializer.Deserialize(invalidPayload));
    }

    [Fact]
    public void SerializeAndDeserialize_DoesNotPersistTransientLiquidState()
    {
        var serializer = new RuntimeEntityBinarySerializer();
        var entities = new[]
        {
            new Entity
            {
                EntityId = 42,
                Type = EntityType.Player,
                Position = new Float2(2f, 3f),
                Velocity = new Float2(1f, -1f),
                LocalBounds = new AabbF(0f, 0f, 1f, 2f),
                IsInLiquid = true,
                Submersion = 0.75f,
                CurrentLiquidType = LiquidKind.Honey
            }
        };

        var payload = serializer.Serialize(entities);
        var restored = serializer.Deserialize(payload);

        var restoredEntity = Assert.Single(restored);
        Assert.False(restoredEntity.IsInLiquid);
        Assert.Equal(0f, restoredEntity.Submersion);
        Assert.Equal(LiquidKind.None, restoredEntity.CurrentLiquidType);
    }
}
