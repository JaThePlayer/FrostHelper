using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;


namespace FrostHelper
{
    /// <summary>
    /// Custom dream blocks except they extend DreamBlock
    /// </summary>
    //[CustomEntity("FrostHelper/CustomDreamBlock")]
    [TrackedAs(typeof(DreamBlock))]
    [Tracked]
    public class CustomDreamBlockV2 : DreamBlock
    {
        // new attributes
        public float DashSpeed;
        public bool AllowRedirects;
        public bool AllowRedirectsInSameDir;
        public float SameDirectionSpeedMultiplier;

        private Color activeBackColor;

        private Color disabledBackColor;

        private Color activeLineColor;

        private Color disabledLineColor;

        public CustomDreamBlockV2(EntityData data, Vector2 offset) : base(data, offset)
        {
            activeBackColor = ColorHelper.GetColor(data.Attr("activeBackColor", "Black"));
            disabledBackColor = ColorHelper.GetColor(data.Attr("disabledBackColor", "1f2e2d"));
            activeLineColor = ColorHelper.GetColor(data.Attr("activeLineColor", "White"));
            disabledLineColor = ColorHelper.GetColor(data.Attr("disabledLineColor", "6a8480"));
            DashSpeed = data.Float("speed", 240f);
            AllowRedirects = data.Bool("allowRedirects");
            AllowRedirectsInSameDir = data.Bool("allowSameDirectionDash");
            SameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1f);
        }


        public override void Render()
        {
            // change the colors
            DreamBlock_activeBackColor.SetValue(null, activeBackColor);
            DreamBlock_disabledBackColor.SetValue(null, disabledBackColor);
            DreamBlock_activeLineColor.SetValue(null, activeLineColor);
            DreamBlock_disabledLineColor.SetValue(null, disabledLineColor);
            base.Render();
            // revert our changes
            DreamBlock_activeBackColor.SetValue(null, baseActiveBackColor);
            DreamBlock_disabledBackColor.SetValue(null, baseDisabledBackColor);
            DreamBlock_activeLineColor.SetValue(null, baseActiveLineColor);
            DreamBlock_disabledLineColor.SetValue(null, baseDisabledLineColor);
        }
        
        private static readonly FieldInfo DreamBlock_activeBackColor = typeof(DreamBlock).GetField("activeBackColor", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo DreamBlock_disabledBackColor = typeof(DreamBlock).GetField("disabledBackColor", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo DreamBlock_activeLineColor = typeof(DreamBlock).GetField("activeLineColor", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo DreamBlock_disabledLineColor = typeof(DreamBlock).GetField("disabledLineColor", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly Color baseActiveBackColor = Color.Black;
        private static readonly Color baseDisabledBackColor = Calc.HexToColor("1f2e2d");
        private static readonly Color baseActiveLineColor = Color.White;
        private static readonly Color baseDisabledLineColor = Calc.HexToColor("6a8480");
    }
}
