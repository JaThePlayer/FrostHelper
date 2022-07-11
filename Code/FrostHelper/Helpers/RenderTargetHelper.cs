namespace FrostHelper;

public static class RenderTargetHelper<T> {
    private static VirtualRenderTarget Instance;

    public static VirtualRenderTarget Get(bool preserve = true, bool useHDleste = true) {
        if (Instance is null || (useHDleste && Instance.Width != GameplayBuffers.Gameplay.Width)) {
            Instance?.Dispose();
            Instance = VirtualContent.CreateRenderTarget($"RenderTarget_{nameof(T)}",
                useHDleste ? GameplayBuffers.Gameplay.Width : 320,
                useHDleste ? GameplayBuffers.Gameplay.Height : 180, 
                false, preserve);
        }

        return Instance;
    }
}
