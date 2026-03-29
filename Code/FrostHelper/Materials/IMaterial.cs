using FrostHelper.Helpers;

namespace FrostHelper.Materials;

internal interface IMaterial : IDisposable {
    /*
    void Fill(RenderTarget2D target, in RenderContext ctx);

    void Fill(Rectangle bounds, Color tint, in RenderContext ctx) {
        using var targetRef = RenderTargetPool.Get(bounds.Width, bounds.Height);
        var target = targetRef.Target;
        Fill(target, ctx);
        Draw.SpriteBatch.Draw(target, bounds, tint);
    }
    */

    void Fill(Rectangle bounds, in RenderContext ctx);
}

internal record struct RenderContext(Vector2 RenderPosition, Camera Camera, Session Session) {
    public static RenderContext CreateFor(Vector2 position, Scene scene) {
        if (scene.MaybeLevel() is {} level) {
            return new RenderContext(position, level.Camera, level.Session);
        }
        
        throw new InvalidOperationException("Cannot create render context from non-level scene.");
    }
}
