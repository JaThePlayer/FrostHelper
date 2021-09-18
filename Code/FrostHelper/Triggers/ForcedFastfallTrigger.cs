using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;

namespace FrostHelper
{
    [Tracked]
    [CustomEntity("FrostHelper/ForcedFastfall")]
    public class ForcedFastfallTrigger : Trigger
    {

        public ForcedFastfallTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
        }

        public static bool IsForcedFastfall(Scene scene)
        {
            foreach (var item in scene.Tracker.GetEntities<ForcedFastfallTrigger>())
            {
                if ((item as Trigger).Triggered)
                    return true;
            }

            return false;
        }

        [OnLoad]
        public static void Load()
        {
            IL.Celeste.Player.NormalUpdate += Player_NormalUpdate;
        }

        [OnUnload]
        public static void Unload()
        {
            IL.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
        }

        private static void Player_NormalUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(160f)))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<Player, float>>((Player player) => 
                { 
                    return !IsForcedFastfall(player.Scene) ? 160f : 240f; 
                });
                break;
            }

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(9)))
            {
                cursor.Emit(OpCodes.Ldarg_0); // this
                cursor.EmitDelegate<Func<Player, float>>((Player player) => {
                    if (!IsForcedFastfall(player.Scene))
                    {
                        float num3 = 160f;
                        float num4 = 240f;
                        if (player.SceneAs<Level>().InSpace)
                        {
                            num3 *= 0.6f;
                            num4 *= 0.6f;
                        }
                        return num3 + (num4 - num3) * 0.5f;
                    }
                    else
                    {
                        return 200f;
                    }
                });
                cursor.Emit(OpCodes.Stloc_S, (byte)9);
                break;
            }
        }
    }
}
