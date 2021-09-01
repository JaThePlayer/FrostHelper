using System;
using System.Xml;
using Celeste;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace FrostHelper.DecalRegistry
{
    public static class Rainbow
    {
        [OnLoad]
        public static void Load()
        {
            IL.Celeste.Decal.Banner.Render += AllowColorChange;
            IL.Celeste.Decal.CoreSwapImage.Render += AllowColorChange;
            IL.Celeste.Decal.DecalImage.Render += AllowColorChange;
            IL.Celeste.Decal.FinalFlagDecalImage.Render += AllowColorChange;

            Celeste.Mod.DecalRegistry.AddPropertyHandler("frosthelper.rainbow", (Decal decal, XmlAttributeCollection attrs) => {
                decal.Add(new DecalRainbowifier());
            });
        }

        [OnUnload]
        public static void Unload()
        {
            IL.Celeste.Decal.Banner.Render -= AllowColorChange;
            IL.Celeste.Decal.CoreSwapImage.Render -= AllowColorChange;
            IL.Celeste.Decal.DecalImage.Render -= AllowColorChange;
            IL.Celeste.Decal.FinalFlagDecalImage.Render -= AllowColorChange;
        }

        private static void AllowColorChange(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White")))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<Component,Color>>((Component self) => {
                    if (self.Entity.Get<DecalRainbowifier>() != null)
                    {
                        return ColorHelper.GetHue(self.Scene, self.Entity.Position);
                    } else
                    {
                        return Color.White;
                    }
                });
                return;
            }
        }

        public class DecalRainbowifier : Component
        {
            public DecalRainbowifier() : base(false, false) { }
        }
    }
}
