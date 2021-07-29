using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using System.Reflection;
using Celeste.Mod.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;

namespace FrostHelper
{
    [Tracked]
    [CustomEntity("FrostHelper/LightningColorTrigger")]
    public class LightningColorTrigger : Trigger
    {
        #region Hooks
        [OnLoad]
        public static void Load()
        {
            On.Celeste.LightningRenderer.Awake += LightningRenderer_Awake;
            On.Celeste.LightningRenderer.Reset += LightningRenderer_Reset;
            IL.Celeste.LightningRenderer.OnBeforeRender += LightningRenderer_OnRenderBloom;
        }

        [OnUnload]
        public static void Unload()
        {
            On.Celeste.LightningRenderer.Awake -= LightningRenderer_Awake;
            On.Celeste.LightningRenderer.Reset -= LightningRenderer_Reset;
            IL.Celeste.LightningRenderer.OnBeforeRender -= LightningRenderer_OnRenderBloom;
        }

        public static string OrDefault(string value, string def)
        {
            if (string.IsNullOrWhiteSpace(value))
                return def;
            return value;
        }

        public static Color OrDefault(Color value, Color def)
        {
            if (value == default)
                return def;
            return value;
        }

        private static void LightningRenderer_OnRenderBloom(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            //while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White")))
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("f7b262")))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<LightningRenderer, string>>((LightningRenderer self) =>
                {
                    LightningColorTrigger trigger;
                    if ((trigger = self.Scene.Tracker.GetEntity<LightningColorTrigger>()) != null && trigger.PlayerIsInside)
                    {
                        return trigger.FillColor;
                    } else
                    {
                        return OrDefault(FrostModule.Session.LightningFillColor, "f7b262");
                    }
                });
            }
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.1f)))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<LightningRenderer, float>>((LightningRenderer self) =>
                {
                    LightningColorTrigger trigger;
                    if ((trigger = self.Scene.Tracker.GetEntity<LightningColorTrigger>()) != null && trigger.PlayerIsInside)
                    {
                        return trigger.FillColorMultiplier;
                    }
                    else
                    {
                        return FrostModule.Session.LightningFillColorMultiplier;
                    }
                });
            }
        }

        private static void LightningRenderer_Awake(On.Celeste.LightningRenderer.orig_Awake orig, LightningRenderer self, Scene scene)
        {
            orig(self, scene);
            if (scene is Level)
            {
                Color[] colors = new Color[2]
                {
                    OrDefault(FrostModule.Session.LightningColorA, Calc.HexToColor("fcf579")),
                    OrDefault(FrostModule.Session.LightningColorB, Calc.HexToColor("8cf7e2")),
                };
                if (colors[0] != Color.White)
                {
                    ChangeLightningColor(self, colors);
                }
            }
        }

        private static void LightningRenderer_Reset(On.Celeste.LightningRenderer.orig_Reset orig, LightningRenderer self)
        {
            if (self.Scene is Level)
            {
                Color[] colors = new Color[2]
                {
                    OrDefault(FrostModule.Session.LightningColorA, Calc.HexToColor("fcf579")),
                    OrDefault(FrostModule.Session.LightningColorB, Calc.HexToColor("8cf7e2")),
                };
                if (colors[0] != Color.White)
                {
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

        private static FieldInfo Bolt_color = null;

        bool persistent;

        public LightningColorTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
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
        
        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            LightningRenderer r = player.Scene.Tracker.GetEntity<LightningRenderer>();
            ChangeLightningColor(r, electricityColors);
            if (persistent)
            {
                var session = SceneAs<Level>().Session;
                //SessionHelper.WriteColorToSession(session, "fh.lightningColorA", electricityColors[0]);
                //SessionHelper.WriteColorToSession(session, "fh.lightningColorB", electricityColors[1]);
                //SessionHelper.WriteColorToSession(session, "fh.lightningBloomColor", BloomColor);
                FrostModule.Session.LightningColorA = electricityColors[0];
                FrostModule.Session.LightningColorB = electricityColors[1];
                FrostModule.Session.LightningFillColor = FillColor;
                FrostModule.Session.LightningFillColorMultiplier = FillColorMultiplier;
            }
        }

        public static void ChangeLightningColor(LightningRenderer renderer, Color[] colors)
        {
            LightningRenderer_electricityColors.SetValue(renderer, colors);
            var bolts = LightningRenderer_bolts.GetValue(renderer);
            List<object> objs = ((IEnumerable<object>)bolts).ToList();
            for (int i = 0; i < objs.Count; i++)
            {
                object obj = objs[i];
                if (Bolt_color == null)
                {
                    Bolt_color = obj.GetType().GetField("color", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                Bolt_color.SetValue(obj, colors[i % 2]);
            }
        }
    }
}
