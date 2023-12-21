using Celeste.Mod.Entities;
using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/BetterShaderTrigger", "FrostHelper/ScreenwideShaderTrigger")]
[Tracked]
public class BetterShaderTrigger : Trigger {
    public string[] Effects;
    public bool Activated;
    public bool Clear;
    public ConditionHelper.Condition Flag;

    public BetterShaderTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Effects = data.Attr("effects").Split(',');

        Activated = data.Bool("alwaysOn", true);
        Clear = data.Bool("clear", false);
        Flag = data.GetCondition("flag", "");
    }

    public override void OnEnter(Player player) {
        Activated = true;
    }

    [OnLoad]
    public static void Load() {
        On.Celeste.Glitch.Apply += Apply_HOOK;
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Glitch.Apply -= Apply_HOOK;
    }

    public static void Apply_HOOK(On.Celeste.Glitch.orig_Apply orig, VirtualRenderTarget source, float timer, float seed, float amplitude) {
        orig(source, timer, seed, amplitude);
        foreach (var trigger in FrostModule.GetCurrentLevel().Tracker.SafeGetEntities<BetterShaderTrigger>()) {
            if (trigger is BetterShaderTrigger s && s.Activated && s.Flag.Check())
                foreach (var item in s.Effects) {
                    Apply(source, source, ShaderHelperIntegration.GetEffect(item), s.Clear);

                }
            return;
        }
    }

    public static void Apply(VirtualRenderTarget source, VirtualRenderTarget target, Effect eff, bool clear = false) {
        ShaderHelperIntegration.ApplyStandardParameters(eff);
        VirtualRenderTarget tempA = GameplayBuffers.TempA;

        Engine.Instance.GraphicsDevice.SetRenderTarget(tempA);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        Draw.SpriteBatch.Draw(source, Vector2.Zero, Color.White);

        GameplayRenderer.End();

        Engine.Instance.GraphicsDevice.SetRenderTarget(target);
        if (clear)
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, eff);
        Draw.SpriteBatch.Draw(tempA, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();
    }

    public static void SimpleApply(VirtualRenderTarget source, VirtualRenderTarget target, Effect eff) {
        SimpleApply((RenderTarget2D)source, target, eff);
    }

    public static void SimpleApply(RenderTarget2D source, VirtualRenderTarget target, Effect eff) {
        ShaderHelperIntegration.ApplyStandardParameters(eff);

        Engine.Instance.GraphicsDevice.SetRenderTarget(target);


        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, eff);
        Draw.SpriteBatch.Draw(source, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();
    }

    public static void SimpleApply(RenderTarget2D source, RenderTargetBinding[] targets, Effect eff) {
        Engine.Instance.GraphicsDevice.SetRenderTargets(targets);


        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, eff);
        Draw.SpriteBatch.Draw(source, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();
    }
}
