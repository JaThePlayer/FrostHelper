using FrostHelper.ModIntegration;

namespace FrostHelper;

public static class RenderTargetHelper<T> {
    private static VirtualRenderTarget Instance;

    public static VirtualRenderTarget Get(bool preserve = true, bool useHDleste = true) {
        if (Instance is null || (useHDleste && Instance.Width != GameplayBuffers.Gameplay.Width)) {
            Instance?.Dispose();
            Instance = VirtualContent.CreateRenderTarget($"FrostHelper.RenderTarget<{nameof(T)}>",
                useHDleste ? GameplayBuffers.Gameplay.Width : 320,
                useHDleste ? GameplayBuffers.Gameplay.Height : 180, 
                false, preserve);
        }

        return Instance;
    }
}

/// <summary>
/// Savestate-safe way to reference a pooled render target.
/// </summary>
internal sealed class VirtualRenderTargetRef(VirtualRenderTarget target) : ISavestatePersisted, IDisposable {
    private VirtualRenderTarget? _target = target;

    public bool Disposed => _target is null;
    
    public VirtualRenderTarget Target {
        get {
            ObjectDisposedException.ThrowIf(_target is null, this);
            return _target;
        }
    }

    private void ReleaseUnmanagedResources() {
        if (_target is { }) {
            RenderTargetHelper.ReturnFullScreenBuffer(_target);
            _target = null!;
        }
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VirtualRenderTargetRef() {
        ReleaseUnmanagedResources();
    }
}

internal static class RenderTargetHelper {
    private static readonly Stack<VirtualRenderTargetRef> _refPool = new();
    private static readonly Stack<VirtualRenderTarget> _pool = new();
    private static int _createdCount;
    
    /// <summary>
    /// Rents out a buffer that fills the whole screen. Make sure to call <see cref="ReturnFullScreenBuffer"/> afterwards.
    /// </summary>
    public static VirtualRenderTargetRef RentFullScreenBufferRef() {
        while (_refPool.TryPop(out var nextRef)) {
            var next = nextRef.Target;
            if (next.Width == GameplayBuffers.Gameplay.Width)
                return nextRef;
            
            // this target is invalid now, clear it
            next.Dispose();
        }
        
        // create a new target
        var newTarget = VirtualContent.CreateRenderTarget($"FrostHelper.RenderTargetHelperRef[{_createdCount++}]", GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height, false, true);

        return new(newTarget);
    }

    public static VirtualRenderTarget RentFullScreenBuffer() {
        while (_pool.TryPop(out var nextRef)) {
            var next = nextRef.Target;
            if (next.Width == GameplayBuffers.Gameplay.Width)
                return nextRef;
            
            // this target is invalid now, clear it
            next.Dispose();
        }
        
        // create a new target
        var newTarget = VirtualContent.CreateRenderTarget($"FrostHelper.RenderTargetHelper[{_createdCount++}]", GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height, false, true);

        return newTarget;
    }

    /// <summary>
    /// Returns a full screen buffer to the pool, so that it can be used again by other places (or next frame)
    /// </summary>
    public static void ReturnFullScreenBuffer(VirtualRenderTarget target) {
        _pool.Push(target);
    }
    
    internal static void ReturnFullScreenBuffer(VirtualRenderTargetRef target) {
        if (!target.Disposed)
            _refPool.Push(new(target.Target));
    }
}
