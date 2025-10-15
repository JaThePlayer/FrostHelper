using Celeste.Mod.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/CustomFlutterBird")]
internal sealed class CustomFlutterBird : FlutterBird {
    public bool DontFlyAway;
    public string FlyAwaySfx, HopSfx;

    public CustomFlutterBird(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();

        Get<Sprite>().Color = Calc.Random.Choose(ColorHelper.GetColors(data.Attr("colors", "89fbff,f0fc6c,f493ff,93baff")));

        DontFlyAway = data.Bool("dontFlyAway", false);
        FlyAwaySfx = data.Attr("flyAwaySfx", "event:/game/general/birdbaby_flyaway");
        HopSfx = data.Attr("hopSfx", "event:/game/general/birdbaby_hop");
        // data.Attr("tweetingSfx", "event:/game/general/birdbaby_tweet_loop"); - not here since it needs to be loaded in the base ctor
        // data.Attr("directory", "scenery/flutterbird/") - not here since it needs to be loaded in the base ctor
    }

    #region Hooks
    [OnLoad]
    public static void Load() {
        // ctor hooks need to be added before the ctor actually runs
        // todo: move this somewhere else?
        IL.Celeste.FlutterBird.ctor += IL_FlutterBird_ctor;
    }

    private static bool _hooksLoaded;
    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        On.Celeste.FlutterBird.FlyAway += FlutterBird_FlyAway;
        IL.Celeste.FlutterBird.FlyAway += IL_FlutterBird_FlyAway;
        FrostModule.RegisterILHook(EasierILHook.HookCoroutine("Celeste.FlutterBird", "IdleRoutine", IL_FlutterBird_IdleRoutine));
    }

    private static void IL_FlutterBird_FlyAway(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.SeekLoadString("event:/game/general/birdbaby_flyaway")) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitCall(GetFlyAwaySfx);
        }
    }

    private static void IL_FlutterBird_IdleRoutine(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.SeekLoadString("event:/game/general/birdbaby_hop")) {
            cursor.Emit(OpCodes.Ldloc_1); // this
            cursor.EmitCall(GetHopSfx);
        }
    }

    [OnUnload]
    public static void Unload() {
        IL.Celeste.FlutterBird.ctor -= IL_FlutterBird_ctor;

        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.FlutterBird.FlyAway -= FlutterBird_FlyAway;
        IL.Celeste.FlutterBird.FlyAway -= IL_FlutterBird_FlyAway;
    }

    private static void IL_FlutterBird_ctor(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdsfld(typeof(GFX), nameof(GFX.SpriteBank))
                  && instr.Next.MatchLdstr("flutterbird")
        )) {
            var postCreate = cursor.DefineLabel();
            var postCustomCreate = cursor.DefineLabel();

            /*
            Change
            this.sprite = GFX.SpriteBank.Create("flutterbird")
            To:
            this.sprite = this is CustomFlutterBird
                          ? CustomCreate(data)
                          : GFX.SpriteBank.Create("flutterbird")
            */

            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Isinst, typeof(CustomFlutterBird));
            cursor.Emit(OpCodes.Brfalse, postCustomCreate);

            cursor.Emit(OpCodes.Ldarg_1); // EntityData
            cursor.EmitCall(CustomCreate);
            cursor.Emit(OpCodes.Br, postCreate);

            cursor.MarkLabel(postCustomCreate);
            // orig sprite bank creation code goes here...

            cursor.SeekVirtFunctionCall<SpriteBank>("Create", MoveType.After);
            cursor.MarkLabel(postCreate);
        }

        if (cursor.SeekLoadString("event:/game/general/birdbaby_tweet_loop")) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldarg_1); // EntityData
            cursor.EmitCall(GetTweetingSfx);
        }
    }

    private static string GetTweetingSfx(string orig, FlutterBird self, EntityData data) {
        if (self is CustomFlutterBird)
            return data.Attr("tweetingSfx", orig);
        return orig;
    }

    private static string GetFlyAwaySfx(string orig, FlutterBird self) {
        if (self is CustomFlutterBird bird) {
            // Make sure the bird doesn't suddenly vanish if it flew off into a different room during a room transition.
            bird.Tag |= Tags.Persistent;
            return bird.FlyAwaySfx;
        }

        return orig;
    }

    private static string GetHopSfx(string orig, FlutterBird self) {
        if (self is CustomFlutterBird bird)
            return bird.HopSfx;
        return orig;
    }

    private static Sprite CustomCreate(EntityData data) {
        var dir = data.Attr("directory", "scenery/flutterbird/");
        if (!dir.EndsWith('/')) {
            dir += "/";
            data.Values["directory"] = dir; // might as well fix up the path to reduce allocations later
        }

        return CustomSpriteHelper.CreateCustomSprite("flutterbird", dir);
    }

    private static void FlutterBird_FlyAway(On.Celeste.FlutterBird.orig_FlyAway orig, FlutterBird self, int direction, float delay) {
        if (self is not CustomFlutterBird { DontFlyAway: true }) {
            orig(self, direction, delay);
        }
    }
    #endregion
}
