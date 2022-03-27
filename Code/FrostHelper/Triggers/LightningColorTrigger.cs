using Celeste.Mod.Entities;

namespace FrostHelper;

[Tracked]
[CustomEntity("FrostHelper/LightningColorTrigger")]
public class LightningColorTrigger : Trigger {
    #region Hooks
    [OnLoad]
    public static void Load() {
        On.Celeste.LightningRenderer.Awake += LightningRenderer_Awake;
        On.Celeste.LightningRenderer.Reset += LightningRenderer_Reset;
        IL.Celeste.LightningRenderer.OnBeforeRender += LightningRenderer_OnBeforeRender;
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.LightningRenderer.Awake -= LightningRenderer_Awake;
        On.Celeste.LightningRenderer.Reset -= LightningRenderer_Reset;
        IL.Celeste.LightningRenderer.OnBeforeRender -= LightningRenderer_OnBeforeRender;
    }

    public static string OrDefault(string? value, string def) {
        if (string.IsNullOrWhiteSpace(value))
            return def;
        return value!;
    }

    public static Color OrDefault(Color? value, Color def) {
        return value.GetValueOrDefault(def);
    }

    private static void LightningRenderer_OnBeforeRender(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        //while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White")))
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("f7b262"))) {
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate<Func<LightningRenderer, string>>((LightningRenderer self) => {
                return GetFillColorString();
            });
        }
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.1f))) {
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate<Func<LightningRenderer, float>>((LightningRenderer self) => {
                return GetLightningFillColorMultiplier();
            });
        }
    }

    internal static LightningColorTrigger? GetFirstEnteredTrigger() {
        foreach (LightningColorTrigger trigger in Engine.Scene.Tracker.GetEntities<LightningColorTrigger>()) {
            if (trigger.PlayerIsInside) {
                return trigger;
            }
        }

        return null;
    }

    internal static string GetFillColorString() {
        LightningColorTrigger? trigger = GetFirstEnteredTrigger();

        return trigger?.FillColor ?? OrDefault(FrostModule.Session.LightningFillColor, "f7b262");
        /*
        if () {
            return trigger.FillColor;
        } else {
            return OrDefault(FrostModule.Session.LightningFillColor, "f7b262");
        }*/
    }

    internal static float GetLightningFillColorMultiplier() {
        /*
        LightningColorTrigger trigger;
        if ((trigger = Engine.Scene.Tracker.GetEntity<LightningColorTrigger>()) != null && trigger.PlayerIsInside) {
            return trigger.FillColorMultiplier;
        } else {
            return FrostModule.Session.LightningFillColorMultiplier;
        }*/
        return GetFirstEnteredTrigger()?.FillColorMultiplier ?? FrostModule.Session.LightningFillColorMultiplier;
    }

    internal static void GetColors(out Color colorA, out Color colorB) {
        colorA = ColorHelper.GetColor(OrDefault(FrostModule.Session?.LightningColorA, "fcf579"));
        colorB = ColorHelper.GetColor(OrDefault(FrostModule.Session?.LightningColorB, "8cf7e2"));
    }

    internal static void GetColors(out Color colorA, out Color colorB, out Color fillColor, out float fillColorMultiplier) {
        GetColors(out colorA, out colorB);
        fillColor = ColorHelper.GetColor(GetFillColorString());
        fillColorMultiplier = GetLightningFillColorMultiplier();
    }

    private static void LightningRenderer_Awake(On.Celeste.LightningRenderer.orig_Awake orig, LightningRenderer self, Scene scene) {
        orig(self, scene);
        //if (scene is Level)
        {
            GetColors(out var a, out var b);
            if (FrostModule.Session.LightningColorA != null) {
                ChangeLightningColor(self, a, b);
            }
        }
    }

    private static void LightningRenderer_Reset(On.Celeste.LightningRenderer.orig_Reset orig, LightningRenderer self) {
        if (self.Scene is Level) {
            GetColors(out var a, out var b);
            if (FrostModule.Session.LightningColorA != null) {
                ChangeLightningColor(self, a, b);
            }
        }
        orig(self);
    }

    #endregion

    private Color[] electricityColors;
    public string FillColor;
    public float FillColorMultiplier;

    private static FieldInfo LightningRenderer_electricityColors = typeof(LightningRenderer).GetField("electricityColors", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo LightningRenderer_bolts = typeof(LightningRenderer).GetField("bolts", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo? Bolt_color = null;

    bool persistent;

    public LightningColorTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        persistent = data.Bool("persistent", false);
        Color c1 = ColorHelper.GetColor(data.Attr("color1", "fcf579"));
        Color c2 = ColorHelper.GetColor(data.Attr("color2", "8cf7e2"));
        FillColor = ColorHelper.ColorToHex(ColorHelper.GetColor(data.Attr("fillColor", "ffffff")));
        FillColorMultiplier = data.Float("fillColorMultiplier", 0.1f);
        electricityColors = new Color[]
        {
                c1, c2
        };
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        ChangeLightningColor(electricityColors);
        if (persistent) {
            FrostModule.Session.LightningColorA = ColorHelper.ColorToHex(electricityColors[0]);
            FrostModule.Session.LightningColorB = ColorHelper.ColorToHex(electricityColors[1]);
            FrostModule.Session.LightningFillColor = FillColor;
            FrostModule.Session.LightningFillColorMultiplier = FillColorMultiplier;
        }
    }

    public static void ChangeLightningColor(LightningRenderer? renderer, Color colorA, Color colorB) {
        if (renderer is null)
            return;

        ChangeLightningColor(renderer, new[] { colorA, colorB });
    }

    public static void ChangeLightningColor(Color colorA, Color colorB) {
        ChangeLightningColor(new[] { colorA, colorB });
    }

    public static void ChangeLightningColor(Color[] colors) {
        ChangeLightningColor(Engine.Scene.Tracker.GetEntity<LightningRenderer>(), colors);

        if (Engine.Scene.Tracker.GetEntity<CustomLightningRenderer>() is { } customRenderer) {
            customRenderer.ElectricityColors = colors;
            var bolts = customRenderer.Bolts;
            for (int i = 0; i < bolts.Count; i++) {
                bolts[i].Color = colors[i % 2];
            }
        }
    }

    public static void ChangeLightningColor(LightningRenderer? renderer, Color[] colors) {
        if (renderer is null)
            return;

        LightningRenderer_electricityColors.SetValue(renderer, colors);
        var bolts = LightningRenderer_bolts.GetValue(renderer);
        var i = 0;
        foreach (var bolt in (IEnumerable<object>) bolts) {
            if (Bolt_color == null) {
                Bolt_color = bolt.GetType().GetField("color", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            Bolt_color.SetValue(bolt, colors[i % 2]);
            i++;
        }
    }
}
