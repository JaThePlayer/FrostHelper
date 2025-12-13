using FrostHelper.ModIntegration;

namespace FrostHelper.Components;

[TrackedAs(typeof(EffectCutout))]
[Tracked]
internal class CustomBloomCutout : EffectCutout {
    #region Hooks
    private static bool _hooksLoaded = false;

    internal static void LoadHooksIfNeeded() {
        if (_hooksLoaded) {
            return;
        }
        _hooksLoaded = true;

        IL.Celeste.BloomRenderer.Apply += BloomRenderer_Apply;
        On.Celeste.EffectCutout.MakeLightsDirty += EffectCutout_MakeLightsDirty;
    }

    [OnUnload]
    internal static void UnloadHooks() {
        if (!_hooksLoaded) {
            return;
        }
        _hooksLoaded = false;

        IL.Celeste.BloomRenderer.Apply -= BloomRenderer_Apply;
        On.Celeste.EffectCutout.MakeLightsDirty -= EffectCutout_MakeLightsDirty;
    }

    private static void EffectCutout_MakeLightsDirty(On.Celeste.EffectCutout.orig_MakeLightsDirty orig, EffectCutout self) {
        if (self is not CustomBloomCutout) {
            orig(self);
        }
    }

    private static void BloomRenderer_Apply(ILContext il) {
        var cursor = new ILCursor(il);

        int cutoutLocalIdx = -1;
        if (cursor.TryGotoNext(MoveType.After, 
            instr => instr.MatchIsinst<EffectCutout>(),
            instr => instr.MatchStloc(out cutoutLocalIdx)
            )) {

            /*
             * EffectCutout effectCutout = component3 as EffectCutout;
             * + RenderCustomEffectCutout(effectCutout)
             */
            cursor.Emit(OpCodes.Ldloc, cutoutLocalIdx);
            cursor.EmitCall(RenderCustomEffectCutout);

            /*
             * - if (effectCutout.Visible)
             * + if (ShouldDrawRectangle(effectCutout.Visible, effectCutout)
             */
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Component>(nameof(Visible)))) {
                cursor.Emit(OpCodes.Ldloc, cutoutLocalIdx);
                cursor.EmitCall(ShouldDrawRectangle);
            }

            /*
                        Draw.SpriteBatch.End();
					}
				}
				Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            	Draw.Rect(-10f, -10f, 340f, 200f, Color.White * this.Base);
				Draw.SpriteBatch.End();
                + CustomBloomBlocker.DrawBloomBlockers()
             */
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<SpriteBatch>("End")))
                return;
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<SpriteBatch>("Begin")))
                return;
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<SpriteBatch>("End")))
                return;

            cursor.EmitCall(CustomBloomBlocker.DrawBloomBlockers);
        }
    }

    private static bool ShouldDrawRectangle(bool visible, EffectCutout cutout) {
        if (cutout is not CustomBloomCutout)
            return visible;

        return false;
    }

    private static void RenderCustomEffectCutout(EffectCutout cutout) {
        if (cutout is not CustomBloomCutout custom)
            return;

        custom.RenderCutout();
    }
    #endregion

    public Action OnRender;

    /// <summary>
    /// Whether this cutout actually works like a blocker, following this rule:
    /// cutoutColor.a > 0 ? 0 : prevColor
    /// </summary>
    //public bool IsBlocker;

    public CustomBloomCutout(Action onRender) : this() {
        OnRender = onRender;
    }

    public CustomBloomCutout() {
        LoadHooksIfNeeded();

        Visible = false;
    }

    public void RenderCutout() => OnRender?.Invoke();
}

[Tracked]
public class CustomBloomBlocker : Component {
    public CustomBloomBlocker() : base(false, false) {
        CustomBloomCutout.LoadHooksIfNeeded();
    }
    public CustomBloomBlocker(Action onRender) : this() {
        OnRender = onRender;
    }

    public Action OnRender;

    public void RenderCutout() => OnRender?.Invoke();

    public static void DrawVertices(VertexPositionColor[] fill, Camera camera, Vector2 parallaxOffset) {
        Draw.SpriteBatch.End();

        var cam = camera.Matrix * Matrix.CreateTranslation(parallaxOffset.X, parallaxOffset.Y, 0f);
        GFX.DrawVertices(cam, fill, fill.Length,
            BloomBlockVertsEffect,
            ReverseCutoutState
        );

        BeginBloomBlockerBatch();
    }

    internal static void BeginBloomBlockerBatch() {
        var level = FrostModule.GetCurrentLevel();
        Camera camera = level.Camera;

        var effect = BloomBlockEffect;
        effect.ApplyStandardParameters(camera);

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, ReverseCutoutState, SamplerState.PointClamp, null, null, effect, camera.Matrix);
    }

    internal static void DrawBloomBlockers() {
        var cutouts = Engine.Scene.Tracker.GetComponents<CustomBloomBlocker>();

        if (cutouts.Count > 0) {
            BeginBloomBlockerBatch();

            foreach (CustomBloomBlocker cutout in cutouts) {
                cutout.RenderCutout();
            }

            Draw.SpriteBatch.End();
        }
    }

    internal static Effect BloomBlockEffect => ShaderHelperIntegration.GetEffect("FrostHelper/bloomBlock");
    internal static Effect BloomBlockVertsEffect => ShaderHelperIntegration.GetEffect("FrostHelper/bloomBlockVerts");

    internal static BlendState ReverseCutoutState = new BlendState {
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.InverseSourceAlpha,
        AlphaBlendFunction = BlendFunction.Add, //BlendFunction.Min,
    };
}