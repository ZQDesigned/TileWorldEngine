using System;
using System.Collections.Generic;
using System.Linq;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Runtime.Queries;
using TileWorld.Engine.Runtime.Tracking;
using TileWorld.Engine.World;
using TileWorld.Engine.World.Cells;
using TileWorld.Engine.World.Chunks;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Liquids;

/// <summary>
/// Simulates chunk-local liquid flow using <see cref="TileCell.LiquidType"/> and <see cref="TileCell.LiquidAmount"/>.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="Runtime.WorldRuntime"/> instead of
/// depending on transient liquid simulation details.
/// </remarks>
internal sealed class LiquidSystem
{
    private const byte MaxLiquidAmount = byte.MaxValue;
    private const int MaxDownwardTransfer = MaxLiquidAmount;
    private const int MaxLateralTransfer = 96;
    private const int MinLateralSourceAmount = 48;
    private const int MinLateralDifferenceForFlow = 12;
    private const int LateralTransferQuantum = 8;
    private readonly DirtyTracker _dirtyTracker;
    private readonly WorldQueryService _queryService;
    private readonly HashSet<ChunkCoord> _dirtyChunks = [];
    private readonly WorldData _worldData;
    private int _stepIndex;

    /// <summary>
    /// Creates a liquid simulation system.
    /// </summary>
    /// <param name="worldData">The mutable world data that stores liquid values.</param>
    /// <param name="queryService">The query service used to inspect solid blockers.</param>
    /// <param name="dirtyTracker">The dirty tracker used to mark chunk persistence and simulation state.</param>
    public LiquidSystem(
        WorldData worldData,
        WorldQueryService queryService,
        DirtyTracker dirtyTracker)
    {
        _worldData = worldData ?? throw new ArgumentNullException(nameof(worldData));
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _dirtyTracker = dirtyTracker ?? throw new ArgumentNullException(nameof(dirtyTracker));
    }

    /// <summary>
    /// Marks the chunk containing a world-tile coordinate, plus loaded neighbors, as liquid-dirty.
    /// </summary>
    /// <param name="coord">The coordinate whose surroundings should be simulated.</param>
    public void MarkDirty(WorldTileCoord coord)
    {
        var chunkCoord = WorldCoordinateConverter.ToChunkCoord(coord);
        MarkChunkDirty(chunkCoord);
        MarkChunkDirty(chunkCoord.Offset(0, -1));
        MarkChunkDirty(chunkCoord.Offset(1, 0));
        MarkChunkDirty(chunkCoord.Offset(0, 1));
        MarkChunkDirty(chunkCoord.Offset(-1, 0));
    }

    /// <summary>
    /// Marks one chunk as liquid-dirty.
    /// </summary>
    /// <param name="coord">The chunk coordinate to mark.</param>
    public void MarkChunkDirty(ChunkCoord coord)
    {
        _dirtyChunks.Add(coord);
        _dirtyTracker.MarkLoadedDirty(coord, ChunkDirtyFlags.LiquidDirty);
    }

    /// <summary>
    /// Marks all currently loaded chunks as liquid-dirty.
    /// </summary>
    public void MarkAllLoadedChunksDirty()
    {
        foreach (var chunk in _worldData.EnumerateLoadedChunks())
        {
            MarkChunkDirty(chunk.Coord);
        }
    }

    /// <summary>
    /// Simulates one bounded liquid update pass across active chunks.
    /// </summary>
    /// <param name="activeChunks">The active chunks that are eligible for simulation work this frame.</param>
    /// <param name="maxChunksPerFrame">The maximum number of dirty chunks simulated this frame.</param>
    /// <returns><see langword="true"/> when at least one liquid cell changed.</returns>
    public bool SimulateStep(IEnumerable<ChunkCoord> activeChunks, int maxChunksPerFrame)
    {
        ArgumentNullException.ThrowIfNull(activeChunks);

        var activeChunkSet = activeChunks.ToHashSet();
        if (activeChunkSet.Count == 0 || _dirtyChunks.Count == 0)
        {
            return false;
        }

        var simulatedChunks = _dirtyChunks
            .Where(activeChunkSet.Contains)
            .OrderBy(static coord => coord.Y)
            .ThenBy(static coord => coord.X)
            .Take(Math.Max(1, maxChunksPerFrame))
            .ToArray();

        var mutated = false;
        foreach (var chunkCoord in simulatedChunks)
        {
            _dirtyChunks.Remove(chunkCoord);
            _dirtyTracker.ClearDirty(chunkCoord, ChunkDirtyFlags.LiquidDirty);

            if (!_worldData.TryGetChunk(chunkCoord, out var chunk))
            {
                continue;
            }

            if (SimulateChunk(chunkCoord, chunk))
            {
                mutated = true;
            }
        }

        if (mutated && simulatedChunks.Length > 0)
        {
            EngineDiagnostics.Trace($"LiquidSystem simulated liquid flow for: {string.Join(", ", simulatedChunks)}.");
        }

        return mutated;
    }

    private bool SimulateChunk(ChunkCoord chunkCoord, Chunk chunk)
    {
        var mutated = false;
        var preferRightFirst = (_stepIndex++ & 1) == 0;
        var chunkOrigin = WorldCoordinateConverter.ToChunkOrigin(chunkCoord);

        for (var localY = ChunkDimensions.Height - 1; localY >= 0; localY--)
        {
            if (preferRightFirst)
            {
                for (var localX = 0; localX < ChunkDimensions.Width; localX++)
                {
                    mutated |= SimulateCell(chunk, chunkOrigin, localX, localY, preferRightFirst);
                }
            }
            else
            {
                for (var localX = ChunkDimensions.Width - 1; localX >= 0; localX--)
                {
                    mutated |= SimulateCell(chunk, chunkOrigin, localX, localY, preferRightFirst);
                }
            }
        }

        return mutated;
    }

    private bool SimulateCell(Chunk chunk, WorldTileCoord chunkOrigin, int localX, int localY, bool preferRightFirst)
    {
        var worldCoord = new WorldTileCoord(chunkOrigin.X + localX, chunkOrigin.Y + localY);
        var sourceCell = chunk.GetCell(localX, localY);
        if (sourceCell.LiquidAmount == 0)
        {
            return false;
        }

        var sourceLiquidType = sourceCell.LiquidType == 0
            ? (byte)LiquidKind.Water
            : sourceCell.LiquidType;
        var sourceAmount = (int)sourceCell.LiquidAmount;
        var mutated = false;

        if (IsBlocking(worldCoord))
        {
            return UpdateSourceCell(chunk, localX, localY, sourceCell, liquidType: 0, liquidAmount: 0);
        }

        if (sourceCell.LiquidType == 0)
        {
            mutated |= UpdateSourceCell(chunk, localX, localY, sourceCell, sourceLiquidType, (byte)sourceAmount);
            sourceCell = chunk.GetCell(localX, localY);
        }

        sourceAmount = TransferToNeighbor(
            worldCoord,
            sourceLiquidType,
            sourceAmount,
            worldCoord.Offset(0, 1),
            maxTransfer: MaxDownwardTransfer,
            equalize: false);

        if (sourceAmount > 1 && ShouldSpreadLaterally(worldCoord, sourceLiquidType, sourceAmount))
        {
            if (preferRightFirst)
            {
                sourceAmount = TransferToNeighbor(
                    worldCoord,
                    sourceLiquidType,
                    sourceAmount,
                    worldCoord.Offset(1, 0),
                    maxTransfer: MaxLateralTransfer,
                    equalize: true);
                sourceAmount = TransferToNeighbor(
                    worldCoord,
                    sourceLiquidType,
                    sourceAmount,
                    worldCoord.Offset(-1, 0),
                    maxTransfer: MaxLateralTransfer,
                    equalize: true);
            }
            else
            {
                sourceAmount = TransferToNeighbor(
                    worldCoord,
                    sourceLiquidType,
                    sourceAmount,
                    worldCoord.Offset(-1, 0),
                    maxTransfer: MaxLateralTransfer,
                    equalize: true);
                sourceAmount = TransferToNeighbor(
                    worldCoord,
                    sourceLiquidType,
                    sourceAmount,
                    worldCoord.Offset(1, 0),
                    maxTransfer: MaxLateralTransfer,
                    equalize: true);
            }
        }

        var sourceChanged = UpdateSourceCell(
            chunk,
            localX,
            localY,
            sourceCell,
            sourceAmount == 0 ? (byte)0 : sourceLiquidType,
            (byte)Math.Clamp(sourceAmount, 0, (int)MaxLiquidAmount));

        return mutated || sourceChanged;
    }

    private int TransferToNeighbor(
        WorldTileCoord sourceCoord,
        byte sourceLiquidType,
        int sourceAmount,
        WorldTileCoord targetCoord,
        int maxTransfer,
        bool equalize)
    {
        if (sourceAmount <= 0 ||
            !_queryService.IsWithinWorldBounds(targetCoord) ||
            !TryGetLoadedCell(targetCoord, out var targetChunk, out var targetLocalCoord, out var targetCell) ||
            IsBlocking(targetCoord))
        {
            return sourceAmount;
        }

        if (targetCell.LiquidAmount > 0 &&
            targetCell.LiquidType != 0 &&
            targetCell.LiquidType != sourceLiquidType)
        {
            return sourceAmount;
        }

        var targetAmount = targetCell.LiquidAmount;
        var capacity = MaxLiquidAmount - targetAmount;
        if (capacity <= 0)
        {
            return sourceAmount;
        }

        var transfer = Math.Min(Math.Min(sourceAmount, capacity), maxTransfer);
        if (equalize)
        {
            var desired = (sourceAmount - targetAmount) / 2;
            if (desired < MinLateralDifferenceForFlow)
            {
                return sourceAmount;
            }

            transfer = Math.Min(transfer, desired);
            transfer = QuantizeLateralTransfer(transfer);
        }

        if (transfer <= 0)
        {
            return sourceAmount;
        }

        var newTargetAmount = targetAmount + transfer;
        if (UpdateSourceCell(
                targetChunk,
                targetLocalCoord.X,
                targetLocalCoord.Y,
                targetCell,
                sourceLiquidType,
                (byte)Math.Clamp(newTargetAmount, 0, (int)MaxLiquidAmount)))
        {
            MarkDirty(sourceCoord);
            MarkDirty(targetCoord);
        }

        return sourceAmount - transfer;
    }

    private bool UpdateSourceCell(
        Chunk chunk,
        int localX,
        int localY,
        TileCell currentCell,
        byte liquidType,
        byte liquidAmount)
    {
        if (currentCell.LiquidType == liquidType &&
            currentCell.LiquidAmount == liquidAmount)
        {
            return false;
        }

        chunk.SetCell(localX, localY, currentCell with
        {
            LiquidType = liquidAmount == 0 ? (byte)0 : liquidType,
            LiquidAmount = liquidAmount
        });

        _dirtyTracker.MarkLoadedDirty(chunk.Coord, ChunkDirtyFlags.SaveDirty | ChunkDirtyFlags.LiquidDirty);
        return true;
    }

    private bool TryGetLoadedCell(
        WorldTileCoord coord,
        out Chunk chunk,
        out Int2 localCoord,
        out TileCell cell)
    {
        var chunkCoord = WorldCoordinateConverter.ToChunkCoord(coord);
        localCoord = WorldCoordinateConverter.ToLocalCoord(coord);
        if (_worldData.TryGetChunk(chunkCoord, out chunk))
        {
            cell = chunk.GetCell(localCoord.X, localCoord.Y);
            return true;
        }

        cell = default;
        return false;
    }

    private bool IsBlocking(WorldTileCoord coord)
    {
        return _queryService.IsSolid(coord);
    }

    private bool ShouldSpreadLaterally(WorldTileCoord sourceCoord, byte sourceLiquidType, int sourceAmount)
    {
        if (sourceAmount < MinLateralSourceAmount)
        {
            return false;
        }

        var belowCoord = sourceCoord.Offset(0, 1);
        if (!_queryService.IsWithinWorldBounds(belowCoord) || IsBlocking(belowCoord))
        {
            return true;
        }

        if (!TryGetLoadedCell(belowCoord, out _, out _, out var belowCell))
        {
            return false;
        }

        if (belowCell.LiquidAmount == MaxLiquidAmount)
        {
            return true;
        }

        if (belowCell.LiquidAmount > 0 &&
            belowCell.LiquidType != 0 &&
            belowCell.LiquidType != sourceLiquidType)
        {
            return true;
        }

        return false;
    }

    private static int QuantizeLateralTransfer(int transfer)
    {
        if (transfer < LateralTransferQuantum)
        {
            return 0;
        }

        return (transfer / LateralTransferQuantum) * LateralTransferQuantum;
    }
}
