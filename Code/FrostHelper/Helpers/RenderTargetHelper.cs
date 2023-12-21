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

internal static class RenderTargetHelper {
    private static readonly Stack<VirtualRenderTarget> _pool = new();
    private static int _createdCount;
    
    /// <summary>
    /// Rents out a buffer that fills the whole screen. Make sure to call <see cref="ReturnFullScreenBuffer"/> afterwards.
    /// </summary>
    public static VirtualRenderTarget RentFullScreenBuffer() {
        while (_pool.TryPop(out var next)) {
            if (next.Width == GameplayBuffers.Gameplay.Width)
                return next;
            
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
}
