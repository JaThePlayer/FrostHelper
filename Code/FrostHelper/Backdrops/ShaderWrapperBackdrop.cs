using FrostHelper.ModIntegration;

namespace FrostHelper.Backdrops;

public class ShaderWrapperBackdrop : Backdrop {
    public string WrappedTag;
    public string ShaderName;

    public ShaderWrapperBackdrop(BinaryPacker.Element child) : base() {
        WrappedTag = child.Attr("wrappedTag", "");
        ShaderName = child.Attr("shader", "");
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

        var gd = Draw.SpriteBatch.GraphicsDevice;
        var backdrops = GetAffectedBackdrops();
        var renderTargets = gd.GetRenderTargets();
        var prevBlendState = gd.BlendState;
        var tempBuffer = RenderTargetHelper<ShaderWrapperBackdrop>.Get();
        var eff = ShaderHelperIntegration.GetEffect(ShaderName);
        ShaderHelperIntegration.ApplyStandardParameters(eff, camera: null);

        Renderer.EndSpritebatch();
        gd.SetRenderTarget(tempBuffer);
        gd.Clear(Color.Transparent);
        //Renderer.StartSpritebatch(BlendState.AlphaBlend);
        //gd.Textures[1] = GFX.MagicGlowNoise.Texture;
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, 
                               null, Matrix.Identity);
        Renderer.usingSpritebatch = true;

        foreach (var backdrop in backdrops) {
            //Console.WriteLine(backdrop);
            backdrop.Visible = true;
            backdrop.Render(scene);
            backdrop.Visible = false;
        }

        Renderer.EndSpritebatch();
        BetterShaderTrigger.SimpleApply(tempBuffer, renderTargets, eff);
        Renderer.StartSpritebatch(prevBlendState);
    }
}
