using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/DontUpdateInvisibleStylegroundsController")]
internal sealed class DontUpdateInvisibleStylegroundsController : Entity {
    internal static readonly Action<Backdrop, Scene> Backdrop_base_Update = EasierILHook.CreateDynamicMethod<Action<Backdrop, Scene>>("Wrapper.base_Update", il => {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, typeof(Backdrop).GetMethod(nameof(Backdrop.Update))!); // use Call instead of Callvirt to call the base method
        il.Emit(OpCodes.Ret);
    });

    internal class ParallaxWrapInfo : IAttachable {
        public static string DynamicDataName => "fh.ParallaxWrapInfo";
        
        public bool Wrapped;
    }

    #region Hooks
    private static bool _hooksLoaded = false;

    public static void LoadParallaxHooksIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        IL.Celeste.Parallax.Update += Parallax_Update1;
    }

    private static bool ShouldUpdate(Parallax self) => self.Visible || !self.GetOrCreateDynamicDataAttached<ParallaxWrapInfo>().Wrapped;// !self.GetAttached<ParallaxWrapInfo>().Wrapped || self.Visible;

    private static void Parallax_Update1(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Backdrop>("Update"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(ShouldUpdate);

            var label = cursor.DefineLabel();
            cursor.Emit(OpCodes.Brtrue, label);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(label);
        }
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        IL.Celeste.Parallax.Update -= Parallax_Update1;
    }
    #endregion

    Type[] AffectedTypes;
    bool bg;

    public DontUpdateInvisibleStylegroundsController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        AffectedTypes = API.API.GetTypes(data.Attr("types", ""));

        bg = data.Bool("bg", false);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        var level = scene as Level;

        // wrap any affected stylegrounds with our own styleground which skips Update when the styleground is invisible
        WrapBackdrops(level!.Foreground.Backdrops);
        if (bg) {
            WrapBackdrops(level.Background.Backdrops);
        }

        RemoveSelf();
    }

    private void WrapBackdrops(List<Backdrop> backdrops) {
        for (int i = backdrops.Count - 1; i >= 0; i--) {
            Backdrop? backdrop = backdrops[i];
            if (AffectedTypes.ContainsReference(backdrop.GetType())) {
                if (backdrop is Parallax p) {
                    LoadParallaxHooksIfNeeded();
                    p.GetOrCreateDynamicDataAttached<ParallaxWrapInfo>().Wrapped = true;
                } else {
                    backdrops[i] = new Wrapper(backdrop);
                }
            }
        }
    }

    public class Wrapper : Backdrop {
        public readonly Backdrop Inner;

        public Wrapper(Backdrop inner) {
            Inner = inner;

            OnlyIfFlag = inner.OnlyIfFlag;
            OnlyIfNotFlag = inner.OnlyIfNotFlag;
            AlsoIfFlag = inner.AlsoIfFlag;
            Dreaming = inner.Dreaming;
            ExcludeFrom = inner.ExcludeFrom;
            OnlyIn = inner.OnlyIn;
            InstantIn = inner.InstantIn;
            InstantOut = inner.InstantOut;
            Tags = inner.Tags;
        }

        public override void Update(Scene scene) {
            Backdrop_base_Update(Inner, scene);
            Visible = Inner.Visible;
            if (Visible) {
                // this will end up calling base.Update(scene) again, oh well
                Inner.Update(scene);
            }
        }

        // Render doesn't get called if !Visible, no extra actions needed
        public override void Render(Scene scene) => Inner.Render(scene);

        public override void BeforeRender(Scene scene) {
            if (Visible) {
                Inner.BeforeRender(scene);
            }
        }

        public override void Ended(Scene scene) => Inner.Ended(scene);
    }
}
