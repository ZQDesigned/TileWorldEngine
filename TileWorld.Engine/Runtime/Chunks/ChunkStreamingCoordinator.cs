using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Storage;
using TileWorld.Engine.World.Coordinates;

namespace TileWorld.Engine.Runtime.Chunks;

/// <summary>
/// Coordinates background chunk prefetch work for non-critical outer-ring chunks.
/// </summary>
/// <remarks>
/// Engine internal infrastructure API. External callers should prefer <see cref="Runtime.WorldRuntime"/> instead of
/// coupling to background chunk prefetch details.
/// </remarks>
internal sealed class ChunkStreamingCoordinator : IDisposable
{
    private readonly ConcurrentQueue<ChunkCoord> _completedOrder = new();
    private readonly ConcurrentDictionary<ChunkCoord, PrefetchedChunkResult> _completedResults = new();
    private readonly Func<ChunkCoord, PrefetchedChunkResult> _resolver;
    private readonly ConcurrentQueue<ChunkCoord> _requestedOrder = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _requestedSet = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly CancellationTokenSource _shutdownTokenSource = new();
    private readonly Thread _workerThread;
    private bool _isDisposed;

    /// <summary>
    /// Creates a chunk streaming coordinator with a background resolution callback.
    /// </summary>
    /// <param name="resolver">The callback that resolves a queued chunk on the background worker.</param>
    public ChunkStreamingCoordinator(Func<ChunkCoord, PrefetchedChunkResult> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _workerThread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "TileWorld.ChunkStreamingCoordinator"
        };
        _workerThread.Start();
    }

    /// <summary>
    /// Attempts to queue a chunk for background prefetch.
    /// </summary>
    /// <param name="coord">The chunk coordinate to queue.</param>
    /// <returns><see langword="true"/> when a new queue entry was created.</returns>
    public bool Queue(ChunkCoord coord)
    {
        ThrowIfDisposed();

        if (!_requestedSet.TryAdd(coord, 0))
        {
            return false;
        }

        _requestedOrder.Enqueue(coord);
        _signal.Set();
        return true;
    }

    /// <summary>
    /// Drains completed prefetch results in arrival order.
    /// </summary>
    /// <returns>The completed prefetch results.</returns>
    public IReadOnlyList<PrefetchedChunkResult> DrainCompleted()
    {
        ThrowIfDisposed();

        var results = new List<PrefetchedChunkResult>();
        while (_completedOrder.TryDequeue(out var coord))
        {
            if (_completedResults.TryRemove(coord, out var result))
            {
                _requestedSet.TryRemove(coord, out _);
                results.Add(result);
            }
        }

        return results;
    }

    /// <summary>
    /// Stops the worker thread and releases background resources.
    /// </summary>
    public void Shutdown()
    {
        if (_isDisposed)
        {
            return;
        }

        _shutdownTokenSource.Cancel();
        _signal.Set();
        _workerThread.Join();
        Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _shutdownTokenSource.Dispose();
        _signal.Dispose();
        _isDisposed = true;
    }

    private void WorkerLoop()
    {
        while (!_shutdownTokenSource.IsCancellationRequested)
        {
            if (!_requestedOrder.TryDequeue(out var coord))
            {
                _signal.WaitOne(TimeSpan.FromMilliseconds(100));
                continue;
            }

            try
            {
                var result = _resolver(coord);
                _completedResults[coord] = result;
                _completedOrder.Enqueue(coord);
            }
            catch (Exception exception)
            {
                _requestedSet.TryRemove(coord, out _);
                EngineDiagnostics.Error(
                    $"ChunkStreamingCoordinator failed while resolving {coord}: {exception}");
            }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    internal readonly record struct PrefetchedChunkResult(
        ChunkCoord Coord,
        ChunkStoragePayload Payload,
        ChunkLoadSource Source);
}
