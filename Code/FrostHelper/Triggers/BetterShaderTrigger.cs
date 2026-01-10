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
    
    private readonly EffectParams _effectParams;

    private string? _startingRoom;

    public BetterShaderTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Effects = data.Attr("effects").Split(',');

        Activated = data.Bool("alwaysOn", true);
        Clear = data.Bool("clear", false);
        Flag = data.GetCondition("flag", "");
        _effectParams = data.Parse("parameters", EffectParams.Empty);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        _startingRoom = SceneAs<Level>().Session.Level;
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
        
        if (FrostModule.TryGetCurrentLevel() is not { } level)
            return;
        
        foreach (var trigger in level.Tracker.SafeGetEntities<BetterShaderTrigger>()) {
            if (trigger is BetterShaderTrigger s && s.Activated && s.Flag.Check() && s._startingRoom == level.Session.Level) {
                
                foreach (var item in s.Effects) {
                    Apply(source, source, ShaderHelperIntegration.GetEffect(item).ApplyParametersFrom(s._effectParams, level.Session), s.Clear);
                }
            }
            //return;
        }
    }

    public static void Apply(VirtualRenderTarget source, VirtualRenderTarget target, Effect eff, bool clear = false) {
        eff.ApplyStandardParameters(Engine.Scene);
        VirtualRenderTarget tempA = GameplayBuffers.TempA;

        Engine.Instance.GraphicsDevice.SetRenderTarget(tempA);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

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
        eff.ApplyStandardParameters(Engine.Scene);

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
