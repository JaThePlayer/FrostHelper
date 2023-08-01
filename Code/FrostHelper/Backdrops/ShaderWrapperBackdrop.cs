using FrostHelper.ModIntegration;

namespace FrostHelper.Backdrops;

public class ShaderWrapperBackdrop : Backdrop {
    public string WrappedTag;
    private Effect Effect;

    public ShaderWrapperBackdrop(BinaryPacker.Element child) : base() {
        WrappedTag = child.Attr("wrappedTag", "");
        Effect = ShaderHelperIntegration.GetEffect(child.Attr("shader", ""));
    }

    public IEnumerable<Backdrop> GetAffectedBackdrops() => Renderer.Backdrops.Where(b => b.Tags.Contains(WrappedTag) && b != this);

    public override void BeforeRender(Scene scene) {
        base.BeforeRender(scene);

        foreach (var backdrop in GetAffectedBackdrops()) {
            backdrop.Visible = false;
        }
    }

    public override void Render(Scene scene) {
        base.Render(scene);

        var backdrops = GetAffectedBackdrops();
        RenderWithShader(Renderer, scene, Effect, backdrops);
    }

    internal static void RenderWithShader(BackdropRenderer renderer, Scene scene, Effect eff, IEnumerable<Backdrop> backdrops, bool fakeVisibility = true) {
        var gd = Draw.SpriteBatch.GraphicsDevice;
        var renderTargets = gd.GetRenderTargets();
        var prevBlendState = gd.BlendState;
        var tempBuffer = RenderTargetHelper<ShaderWrapperBackdrop>.Get();
        ShaderHelperIntegration.ApplyStandardParameters(eff);

        renderer.EndSpritebatch();
        gd.SetRenderTarget(tempBuffer);
        gd.Clear(Color.Transparent);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone,
                               null, Matrix.Identity);
        renderer.usingSpritebatch = true;

        foreach (var backdrop in backdrops) {
            var prevVisible = backdrop.Visible;
            if (fakeVisibility)
                backdrop.Visible = true;

            if (backdrop.Visible) {
                backdrop.Renderer = renderer;
                backdrop.Render(scene);
            }

            if (fakeVisibility)
                backdrop.Visible = prevVisible;
        }

        renderer.EndSpritebatch();
        BetterShaderTrigger.SimpleApply(tempBuffer, renderTargets, eff);
        renderer.StartSpritebatch(prevBlendState);
    }
}
