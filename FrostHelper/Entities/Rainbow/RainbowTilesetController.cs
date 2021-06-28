using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Linq;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/RainbowTilesetController")]
    [Tracked]
    public class RainbowTilesetController : Entity
    {
        #region Hooks
        [OnLoad]
        public static void Load()
        {
            IL.Monocle.TileGrid.RenderAt += TileGrid_RenderAt;
        }

        [OnUnload]
        public static void Unload()
        {
            IL.Monocle.TileGrid.RenderAt -= TileGrid_RenderAt;
        }

        private static void TileGrid_RenderAt(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            //cursor.Emit(OpCodes.Ldarg_0); // this
            //cursor.EmitDelegate<Func<TileGrid, RainbowTilesetController>>((TileGrid self) => self.Scene.Tracker.GetEntity<RainbowTilesetController>());
            //cursor.Emit(OpCodes.Stloc_S, (byte)5);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<MTexture>("Draw")))
            {
                cursor.Index--; // go back to the last step, which is when the color is loaded
                cursor.Emit(OpCodes.Ldloc_3); // x
                cursor.Emit(OpCodes.Ldloc_S, (byte)4); // y
                cursor.Emit(OpCodes.Ldarg_0); // this
                //cursor.Emit(OpCodes.Ldloc_S, (byte)5); // controller
                cursor.EmitDelegate<Func<Color, int, int, TileGrid/*, RainbowTilesetController*/, Color>>((Color c, int x, int y, TileGrid self/*, RainbowTilesetController controller*/) => {
                    RainbowTilesetController controller = self.Scene.Tracker.GetEntity<RainbowTilesetController>();
                    if (controller != null && controller.TilesetTexturePaths.Contains(self.Tiles[x, y].Parent.AtlasPath))
                        return ColorHelper.GetHue(Engine.Scene, new Vector2(x * self.TileWidth + self.Position.X + self.Entity.Position.X, y * self.TileWidth + self.Position.Y + self.Entity.Position.Y));

                    return c;
                });
                return;
            }
        }
        #endregion

        public char[] TilesetIDs;
        public string[] TilesetTexturePaths;
        public bool BG;

        public RainbowTilesetController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            BG = data.Bool("bg", false);
            TilesetIDs = FrostModule.GetCharArrayFromCommaSeparatedList(data.Attr("tilesets"));

            TilesetTexturePaths = new string[TilesetIDs.Length];
            for (int i = 0; i < TilesetIDs.Length; i++)
            {
                var autotiler = BG ? GFX.BGAutotiler : GFX.FGAutotiler;
                TilesetTexturePaths[i] = autotiler.GenerateMap(new VirtualMap<char>(new char[,] { { TilesetIDs[i] } }), true).TileGrid.Tiles[0,0].Parent.AtlasPath;
            }
        }
    }
}
