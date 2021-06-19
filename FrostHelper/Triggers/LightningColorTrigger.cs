using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using System.Reflection;
using Celeste.Mod.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/LightningColorTrigger")]
    public class LightningColorTrigger : Trigger
    {
        #region Hooks
        [OnLoad]
        public static void Load()
        {
            On.Celeste.LightningRenderer.Update += LightningRenderer_Update;
            On.Celeste.LightningRenderer.Awake += LightningRenderer_Awake;
            On.Celeste.LightningRenderer.Reset += LightningRenderer_Reset;
        }

        [OnUnload]
        public static void Unload()
        {
            On.Celeste.LightningRenderer.Update -= LightningRenderer_Update;
            On.Celeste.LightningRenderer.Awake -= LightningRenderer_Awake;
            On.Celeste.LightningRenderer.Reset -= LightningRenderer_Reset;
        }

        private static void LightningRenderer_Update(On.Celeste.LightningRenderer.orig_Update orig, LightningRenderer self)
        {
            orig(self);
            // Update any coroutines
            foreach (Component c in self.Components)
            {
                c.Update();
            }
        }

        private static void LightningRenderer_Awake(On.Celeste.LightningRenderer.orig_Awake orig, LightningRenderer self, Scene scene)
        {
            orig(self, scene);
            if (scene is Level)
            {
                var session = (scene as Level).Session;
                Color[] colors = new Color[2]
                {
                SessionHelper.ReadColorFromSession(session, "fh.lightningColorA", Color.White),
                SessionHelper.ReadColorFromSession(session, "fh.lightningColorB", Color.White)
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
                var session = (self.Scene as Level).Session;
                Color[] colors = new Color[2]
                {
                SessionHelper.ReadColorFromSession(session, "fh.lightningColorA", Color.White),
                SessionHelper.ReadColorFromSession(session, "fh.lightningColorB", Color.White)
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

        private static FieldInfo LightningRenderer_electricityColors = typeof(LightningRenderer).GetField("electricityColors", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo LightningRenderer_bolts = typeof(LightningRenderer).GetField("bolts", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo Bolt_color = null;

        bool rainbow;

        bool persistent;

        public LightningColorTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            persistent = data.Bool("persistent", false);
            Color c1 = data.HexColor("color1", Calc.HexToColor("fcf579"));
            Color c2 = data.HexColor("color2", Calc.HexToColor("8cf7e2"));
            electricityColors = new Color[]
            {
                c1, c2
            };
            rainbow = data.Bool("rainbow");
        }
        
        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            LightningRenderer r = player.Scene.Tracker.GetEntity<LightningRenderer>();
            ChangeLightningColor(r, electricityColors);
            if (persistent)
            {
                var session = SceneAs<Level>().Session;
                SessionHelper.WriteColorToSession(session, "fh.lightningColorA", electricityColors[0]);
                SessionHelper.WriteColorToSession(session, "fh.lightningColorB", electricityColors[1]);
            }
            if (rainbow)
            {
                Coroutine c = r.Get<Coroutine>();
                if (c != null)
                {
                    r.Remove(c);
                }
                r.Add(new Coroutine(RainbowElectricityRoutine(r, electricityColors)));
            }
        }

        public IEnumerator RainbowElectricityRoutine(LightningRenderer renderer, Color[] colors)
        {
            colors = new Color[]
            {
                Calc.HexToColor("400000"),
                Calc.HexToColor("900000")
            };
            while(renderer != null)
            {
                // rainbow
                for (int i = 0; i < colors.Length; i++)
                {
                    if (colors[i].R > 0 && colors[i].B == 0)
                    {
                        colors[i].R -= 1;
                        colors[i].G += 1;
                    }
                    if (colors[i].G > 0 && colors[i].R == 0)
                    {
                        colors[i].G -= 1;
                        colors[i].B += 1;
                    }
                    if (colors[i].B > 0 && colors[i].G == 0)
                    {
                        colors[i].R += 1;
                        colors[i].B -= 1;
                    }
                }
                var bolts = LightningRenderer_bolts.GetValue(renderer);
                List<object> objs = ((IEnumerable<object>)bolts).Cast<object>().ToList();
                for (int i = 0; i < objs.Count; i++)
                {
                    object obj = objs[i];
                    if (Bolt_color == null)
                    {
                        Bolt_color = obj.GetType().GetField("color", BindingFlags.Instance | BindingFlags.NonPublic);
                    }
                    Bolt_color.SetValue(obj, colors[i % 2]);
                }
                LightningRenderer_electricityColors.SetValue(renderer, colors);
                yield return null;
            }
            yield break;
        }

        public static void ChangeLightningColor(LightningRenderer renderer, Color[] colors)
        {
            LightningRenderer_electricityColors.SetValue(renderer, colors);
            var bolts = LightningRenderer_bolts.GetValue(renderer);
            List<object> objs = ((IEnumerable<object>)bolts).Cast<object>().ToList();
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
