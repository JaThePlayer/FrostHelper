using MonoMod.ModInterop;

namespace FrostHelper.ModIntegration;

[ModImportName("MotionSmoothing")]
public static class MotionSmoothingIntegration {
    public static void Load() {
        typeof(MotionSmoothingIntegration).ModInterop();
    }

    // [1.5.6+] The scale of the current render target relative to its vanilla size:
    // 1 normally, or Motion Smoothing's hires scale (usually 6) when it has upscaled it.
    public static Func<float>? GetCurrentRenderTargetScale;

    // [1.8.6+] Runs a full-screen shader pass that draws a buffer back over itself. Inside it,
    // an upscaled buffer drawn into another is drawn in game pixels, so a vertex shader should
    // project from the game's dimensions rather than the upscaled viewport.
    public static Action<Action>? RenderAtGameScale;
}
