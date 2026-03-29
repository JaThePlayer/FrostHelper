using System.Threading;

namespace FrostHelper.Helpers;

internal static class RenderTargetPool {
    private static readonly List<RenderTargetPoolRef> Items = new();

    private static int _registeredCleanup;
    
    /// <summary>
    /// Rents a render target from the pool. Make sure to do `using var x = Get(...)`,
    /// to remove the reference from the tracker once you're done with the object.
    /// </summary>
    public static RenderTargetPoolRef Get(int w, int h) {
        if (Interlocked.CompareExchange(ref _registeredCleanup, 1, 0) == 0) {
            BackgroundTaskHelper.RegisterOnInterval(TimeSpan.FromSeconds(10), Cleanup);
        }
        
        Point size = new(w, h);
        lock (Items) {
            foreach (var item in Items) {
                if (!item.HasReference && size == item.Size) {
                    item.AddRef();
                    return item;
                }
            }

            var target = new RenderTarget2D(Draw.SpriteBatch.GraphicsDevice, w, h);
            var newRef = new RenderTargetPoolRef(target);
            newRef.AddRef();
            Items.Add(newRef);

            return newRef;
        }
    }

    private static void Cleanup() {
        lock (Items) {
            var time = DateTimeOffset.Now;
            var items = Items;

            // Even if a render target has no remaining references, we'll still keep it around
            // for a few seconds, so that it can be re-used next frame.
            for (int i = items.Count - 1; i >= 0; i--) {
                var item = items[i];

                if (!item.HasReference && time - item.LastAccessTime > TimeSpan.FromSeconds(2)) {
                    Logger.Verbose("FrostHelper.RenderTargetPool", $"Cleaned up Render Target {i}.");
                    items.RemoveAt(i);
                    item.DisposeBuffer();
                }
            }
        }
    }
}

/// <summary>
/// Provides a reference to a <see cref="RenderTarget2D"/> from the <see cref="RenderTargetPool"/>.
/// This class is reference-counted and needs to be disposed properly after each use.
/// </summary>
public sealed class RenderTargetPoolRef : IDisposable {
    private RenderTarget2D? _target;
    private DateTimeOffset _lastAccess;
    private int _refCount;
    
    public bool HasReference => _refCount > 0;

    public bool Disposed => _target == null;

    internal RenderTargetPoolRef(RenderTarget2D target) {
        _target = target;
        UpdateLastAccessTime();
    }
    
    public RenderTarget2D Target {
        get {
            UpdateLastAccessTime();
            return _target ?? throw new ObjectDisposedException(nameof(RenderTargetPoolRef));
        }
    }

    internal DateTimeOffset LastAccessTime
        => _lastAccess;

    internal void AddRef() {
        Interlocked.Increment(ref _refCount);
        UpdateLastAccessTime();
    }

    private void RemoveRef() {
        Interlocked.Decrement(ref _refCount);
    }

    private void UpdateLastAccessTime() {
        _lastAccess = DateTimeOffset.Now;
    }

    internal Point Size {
        get {
            var target = _target ?? throw new ObjectDisposedException(nameof(RenderTargetPoolRef));

            return new(target.Width, target.Height);
        }
    }

#pragma warning disable CA1816
    public void Dispose() {
#pragma warning restore CA1816
        RemoveRef();
    }

    internal void DisposeBuffer() {
        _target?.Dispose();
        _target = null!;
    }

    ~RenderTargetPoolRef() {
        DisposeBuffer();
    }
}