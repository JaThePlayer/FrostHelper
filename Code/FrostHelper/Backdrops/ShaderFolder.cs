using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper.Backdrops;

internal class ShaderFolder : Backdrop {
    protected readonly EffectRef Effect;
    protected readonly ConditionHelper.Condition Condition;
    
    protected List<Backdrop> Inner { get; private init; }

    public static ShaderFolder CreateWithInnerStyles(MapData map, BinaryPacker.Element element) {
        var folder = new ShaderFolder(element) {
            Inner = map.CreateBackdrops(element)
        };

        return folder;
    }
    
    public ShaderFolder(BinaryPacker.Element element) {
        Effect = ShaderHelperIntegration.GetEffectRef(element.Attr("shader", ""));
        Condition = new(element.Attr("shaderFlag"));
        
        Inner = [];
    }

    public bool IsShaderEnabled() => Condition.Check();

    public override void Update(Scene scene) {
        base.Update(scene);

        if (Visible) {
            foreach (var item in Inner) {
                item.Update(scene);
            }
        }
    }

    public override void BeforeRender(Scene scene) {
        base.BeforeRender(scene);

        if (Visible) {
            foreach (var item in Inner) {
                item.BeforeRender(scene);
            }
        }
    }

    public override void Ended(Scene scene) {
        base.Ended(scene);

        foreach (var item in Inner) {
            item.Ended(scene);
        }
    }

    public override void Render(Scene scene) {
        base.Render(scene);

        if (IsShaderEnabled()) {
            RenderWithShader(Renderer, scene, Effect.Get(), Inner, fakeVisibility: false);
        } else {
            RenderStyles(Renderer, scene, Inner, fakeVisibility: false);
        }
    }

    internal static void RenderStyles(BackdropRenderer renderer, Scene scene, List<Backdrop> backdrops, bool fakeVisibility) {
        var oldBackdrops = renderer.Backdrops;
        renderer.Backdrops = backdrops;
        if (fakeVisibility)
            foreach (var backdrop in renderer.Backdrops) {
                backdrop.Visible = true;
            }

        renderer.Render(scene);
        if (fakeVisibility)
            foreach (var backdrop in renderer.Backdrops) {
                backdrop.Visible = false;
            }
        renderer.Backdrops = oldBackdrops;
    }
    
    internal static void RenderWithShader(BackdropRenderer renderer, Scene scene, Effect eff, List<Backdrop> backdrops, bool fakeVisibility = true) {
        if (backdrops.Count == 0)
            return;
        
        var gd = Draw.SpriteBatch.GraphicsDevice;
        var renderTargets = gd.GetRenderTargets();
        var prevBlendState = gd.BlendState;
        var tempBuffer = RenderTargetHelper.RentFullScreenBuffer();
        
        eff.ApplyStandardParameters();

        renderer.EndSpritebatch();
        gd.SetRenderTarget(tempBuffer);
        gd.Clear(Color.Transparent);

        RenderStyles(renderer, scene, backdrops, fakeVisibility);

        BetterShaderTrigger.SimpleApply(tempBuffer, renderTargets, eff);
        renderer.StartSpritebatch(prevBlendState);
        
        RenderTargetHelper.ReturnFullScreenBuffer(tempBuffer);
    }
}
