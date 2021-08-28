using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

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
            //IL.Monocle.TileGrid.RenderAt += TileGrid_RenderAt;
            On.Monocle.TileGrid.RenderAt += TileGrid_RenderAt1;
        }

        private static bool contains(MTexture[] arr, MTexture check)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (ReferenceEquals(arr[i], check))
                    return true;
            }
            return false;
        }

        private static void TileGrid_RenderAt1(On.Monocle.TileGrid.orig_RenderAt orig, TileGrid self, Vector2 position)
        {
            var controller = self.Scene.Tracker.GetEntity<RainbowTilesetController>();
            if (controller is null || self.Alpha <= 0f)
            {
                orig(self, position);
                return;
            }
            /*v5
            Rectangle clippedRenderTiles = self.GetClippedRenderTiles();

            int tileWidth = self.TileWidth;
            int tileHeight = self.TileHeight;

            Color color = self.Color * self.Alpha;
            Vector2 renderPos = new Vector2(position.X + clippedRenderTiles.Left * tileWidth, position.Y + clippedRenderTiles.Top * tileHeight);
            for (int i = clippedRenderTiles.Left; i < clippedRenderTiles.Right; i++)
            {
                for (int j = clippedRenderTiles.Top; j < clippedRenderTiles.Bottom; j++)
                {
                    MTexture mtexture = self.Tiles.orig_get_Item(i, j);
                    if (mtexture != null)
                    {
                        //mtexture.Draw(renderPos, Vector2.Zero, color);
                        Draw.SpriteBatch.Draw(mtexture.Texture.Texture, renderPos, mtexture.ClipRect, color);
                    }
                    renderPos.Y += tileHeight;
                }
                renderPos.X += tileWidth;
                renderPos.Y = position.Y + clippedRenderTiles.Top * tileHeight;
            }*/
            ColorHelper.SetGetHueScene(Engine.Scene);

            Rectangle clippedRenderTiles = self.GetClippedRenderTiles();

            int tileWidth = self.TileWidth;
            int tileHeight = self.TileHeight;

            Color color = self.Color * self.Alpha;
            Vector2 renderPos = new Vector2(position.X + clippedRenderTiles.Left * tileWidth, position.Y + clippedRenderTiles.Top * tileHeight);
            for (int i = clippedRenderTiles.Left; i < clippedRenderTiles.Right; i++)
            {
                for (int j = clippedRenderTiles.Top; j < clippedRenderTiles.Bottom; j++)
                {
                    MTexture mtexture = self.Tiles[i, j];
                    if (mtexture != null)
                    {
                        //Color c = color;
                        if (contains(controller.TilesetTextures, mtexture.Parent))
                        {
                            //c = ColorHelper.GetHue(renderPos);
                            Draw.SpriteBatch.Draw(mtexture.Texture.Texture, renderPos, mtexture.ClipRect, ColorHelper.GetHue(renderPos));
                        } else
                        {
                            Draw.SpriteBatch.Draw(mtexture.Texture.Texture, renderPos, mtexture.ClipRect, color);
                        }
                        
                    }
                    renderPos.Y += tileHeight;
                }
                renderPos.X += tileWidth;
                renderPos.Y = position.Y + clippedRenderTiles.Top * tileHeight;
            }
        }

        [OnUnload]
        public static void Unload()
        {
            IL.Monocle.TileGrid.RenderAt -= TileGrid_RenderAt;
            On.Monocle.TileGrid.RenderAt -= TileGrid_RenderAt1;
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
                    var controllers = self.Scene.Tracker.GetEntities<RainbowTilesetController>();
                    foreach (var e in controllers)
                    {
                        //if (e is RainbowTilesetController controller && controller.TilesetTexturePaths.Contains(self.Tiles[x, y].Parent.AtlasPath))
                        if (e is RainbowTilesetController controller && controller.TilesetTextures.Contains(self.Tiles[x, y].Parent))
                            return ColorHelper.GetHue(Engine.Scene, new Vector2(x * self.TileWidth + self.Position.X + self.Entity.Position.X, y * self.TileWidth + self.Position.Y + self.Entity.Position.Y));
                    }
                    return c;
                });
                return;
            }
        }
        #endregion

        public char[] TilesetIDs;
        //public string[] TilesetTexturePaths;
        public MTexture[] TilesetTextures;
        public bool BG;

        public RainbowTilesetController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            BG = data.Bool("bg", false);
            var all = data.Attr("tilesets") == "*";
            var autotiler = BG ? GFX.BGAutotiler : GFX.FGAutotiler;
            Tag = Tags.Persistent;

            if (!all)
            {
                TilesetIDs = FrostModule.GetCharArrayFromCommaSeparatedList(data.Attr("tilesets"));

                TilesetTextures = new MTexture[TilesetIDs.Length];
                for (int i = 0; i < TilesetTextures.Length; i++)
                {
                    TilesetTextures[i] = autotiler.GenerateMap(new VirtualMap<char>(new char[,] { { TilesetIDs[i] } }), true).TileGrid.Tiles[0, 0].Parent;
                }
            }
            else
            {
                // Autotiler.lookup is Dictionary<char, Autotiler.TerrainType>
                // Autotiler.TerrainType is private, let's do some trickery
                var autotilerLookupKeys = (Autotiler_lookup.GetValue(autotiler) as IDictionary).Keys;
                TilesetTextures = new MTexture[autotilerLookupKeys.Count];
                var enumerator = autotilerLookupKeys.GetEnumerator();
                for (int i = 0; i < TilesetTextures.Length; i++)
                {
                    enumerator.MoveNext();
                    TilesetTextures[i] = autotiler.GenerateMap(new VirtualMap<char>(new char[,] { { (char)enumerator.Current } }), true).TileGrid.Tiles[0, 0].Parent;
                }
            }
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            var controllers = Scene.Tracker.GetEntities<RainbowTilesetController>();
            if (controllers.Count > 1)
            {
                var first = controllers.First(c => c.Scene == scene) as RainbowTilesetController;
                if (first != this)
                {
                    first.TilesetTextures = first.TilesetTextures.Union(TilesetTextures).ToArray();
                    RemoveSelf();
                }
                
            }
        }

        // Dictionary<char, Autotiler.TerrainType>
        private static FieldInfo Autotiler_lookup = typeof(Autotiler).GetField("lookup", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
