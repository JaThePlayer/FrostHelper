using Celeste.Mod.Entities;
using FrostHelper.ModIntegration;

namespace FrostHelper;

/// <summary>
/// Should really be a part of shader helper
/// </summary>
[CustomEntity("FrostHelper/BetterShaderTrigger")]
[Tracked]
public class BetterShaderTrigger : Trigger {

    public string[] Effects;

    public bool Activated;

    public BetterShaderTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Effects = data.Attr("effects").Split(',');

        Activated = data.Bool("alwaysOn", true);
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
        foreach (var trigger in FrostModule.GetCurrentLevel().Tracker.GetEntities<BetterShaderTrigger>()) {
            if (trigger is BetterShaderTrigger s && s.Activated)
                foreach (var item in s.Effects) {
                    Apply(source, source, ShaderHelperIntegration.GetEffect(item));

                }
            return;
        }


    }



    public static void Apply(VirtualRenderTarget source, VirtualRenderTarget target, Effect eff) {
        ShaderHelperIntegration.ApplyStandardParameters(eff);
        VirtualRenderTarget tempA = GameplayBuffers.TempA;

        Engine.Instance.GraphicsDevice.SetRenderTarget(tempA);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        Draw.SpriteBatch.Draw(source, Vector2.Zero, Color.White);

        GameplayRenderer.End();


        //FrostModule.GetCurrentLevel().Bloom.Apply(GameplayBuffers.TempB, FrostModule.GetCurrentLevel());
        //Engine.Instance.GraphicsDevice.Textures[1] = GameplayBuffers.TempB;

        Engine.Instance.GraphicsDevice.SetRenderTarget(target);


        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, eff);
        Draw.SpriteBatch.Draw(tempA, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();
    }

    public static void SimpleApply(VirtualRenderTarget source, VirtualRenderTarget target, Effect eff) {
        ShaderHelperIntegration.ApplyStandardParameters(eff);

        Engine.Instance.GraphicsDevice.SetRenderTarget(target);


        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, eff);
        Draw.SpriteBatch.Draw(source, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();
    }
}
