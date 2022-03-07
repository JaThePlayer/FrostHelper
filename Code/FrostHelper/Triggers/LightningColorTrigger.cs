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

    public static string OrDefault(string value, string def) {
        if (string.IsNullOrWhiteSpace(value))
            return def;
        return value;
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
                LightningColorTrigger trigger;
                if ((trigger = self.Scene.Tracker.GetEntity<LightningColorTrigger>()) != null && trigger.PlayerIsInside) {
                    return trigger.FillColor;
                } else {
                    return OrDefault(FrostModule.Session.LightningFillColor, "f7b262");
                }
            });
        }
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.1f))) {
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate<Func<LightningRenderer, float>>((LightningRenderer self) => {
                LightningColorTrigger trigger;
                if ((trigger = self.Scene.Tracker.GetEntity<LightningColorTrigger>()) != null && trigger.PlayerIsInside) {
                    return trigger.FillColorMultiplier;
                } else {
                    return FrostModule.Session.LightningFillColorMultiplier;
                }
            });
        }
    }

    private static Color[] getColors() {
        return new Color[2]
            {
                    ColorHelper.GetColor(OrDefault(FrostModule.Session.LightningColorA, "fcf579")),
                    ColorHelper.GetColor(OrDefault(FrostModule.Session.LightningColorB, "8cf7e2")),
            };
    }

    private static void LightningRenderer_Awake(On.Celeste.LightningRenderer.orig_Awake orig, LightningRenderer self, Scene scene) {
        orig(self, scene);
        //if (scene is Level)
        {
            Color[] colors = getColors();
            if (FrostModule.Session.LightningColorA != null) {
                ChangeLightningColor(self, colors);
            }
        }
    }

    private static void LightningRenderer_Reset(On.Celeste.LightningRenderer.orig_Reset orig, LightningRenderer self) {
        if (self.Scene is Level) {
            Color[] colors = getColors();
            if (FrostModule.Session.LightningColorA != null) {
                ChangeLightningColor(self, colors);
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
        LightningRenderer r = player.Scene.Tracker.GetEntity<LightningRenderer>();
        ChangeLightningColor(r, electricityColors);
        if (persistent) {
            FrostModule.Session.LightningColorA = ColorHelper.ColorToHex(electricityColors[0]);
            FrostModule.Session.LightningColorB = ColorHelper.ColorToHex(electricityColors[1]);
            FrostModule.Session.LightningFillColor = FillColor;
            FrostModule.Session.LightningFillColorMultiplier = FillColorMultiplier;
        }
    }

    public static void ChangeLightningColor(LightningRenderer renderer, Color[] colors) {
        LightningRenderer_electricityColors.SetValue(renderer, colors);
        var bolts = LightningRenderer_bolts.GetValue(renderer);
        List<object> objs = ((IEnumerable<object>) bolts).ToList();
        for (int i = 0; i < objs.Count; i++) {
            object obj = objs[i];
            if (Bolt_color == null) {
                Bolt_color = obj.GetType().GetField("color", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            Bolt_color.SetValue(obj, colors[i % 2]);
        }
    }
}
