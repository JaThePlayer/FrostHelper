namespace FrostHelper;

public static class RenderTargetHelper<T> {
    private static VirtualRenderTarget Instance;

    public static VirtualRenderTarget Get(bool preserve = true) {
        Instance ??= VirtualContent.CreateRenderTarget($"RenderTarget_{nameof(T)}", GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height, false, preserve);

        return Instance;
    }
}
