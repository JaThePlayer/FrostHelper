using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Collections.Generic;

namespace FrostHelper {
    static class OutlineHelper {
        static Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// ONLY CALL IN RENDER!
        /// Cursed for now
        /// </summary>
        public static Texture2D Get(string path, bool inRender = true) {
            if (cache.ContainsKey(path))
                return cache[path];

            MTexture mTexture = GFX.Game[path];
            RenderTarget2D target = new RenderTarget2D(Engine.Graphics.GraphicsDevice, mTexture.Width + 2 + (int) mTexture.DrawOffset.X, mTexture.Height + 2 + (int) mTexture.DrawOffset.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            var prevTarget = Draw.SpriteBatch.GraphicsDevice.GetRenderTargets();
            if (inRender)
                Draw.SpriteBatch.End();
            Draw.SpriteBatch.Begin();
            Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(target);
            Draw.SpriteBatch.GraphicsDevice.Clear(Color.Transparent);

            Texture2D texture = mTexture.Texture.Texture_Safe;
            Rectangle? clipRect = new Rectangle?(mTexture.ClipRect);
            float scaleFix = mTexture.ScaleFix;
            Vector2 origin = (Vector2.Zero - mTexture.DrawOffset) / scaleFix;
            Vector2 drawPos = Vector2.One;
            Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitY, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitY, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitX, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitX, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.End();

            if (inRender) {
                GameplayRenderer.Begin();
            }
            Draw.SpriteBatch.GraphicsDevice.SetRenderTargets(prevTarget);

            var t2d = new Texture2D(Engine.Graphics.GraphicsDevice, target.Width, target.Height, false, SurfaceFormat.Color);
            Color[] buffer = new Color[target.Width * target.Height];
            target.GetData(buffer);
            t2d.SetData(buffer);

            cache.Add(path, t2d);
            target.Dispose();
            return t2d;
        }

        public static Texture2D Get(Image image, bool inRender = true) {
            return Get(image.Texture.AtlasPath ?? image.Texture.Parent.AtlasPath, inRender);
        }

        public static void Dispose() {
            foreach (var item in cache) {
                item.Value.Dispose();
            }
            cache = new Dictionary<string, Texture2D>();
        }

        public static void RenderOutline(Image image, Color color, bool centeredOrigin) {
            var outline = Get(image);
            float scaleFix = image.Texture.ScaleFix;
            Vector2 drawPos = image.RenderPosition;// - new Vector2(1f, 1f).Rotate(image.Rotation);
            float rotation = image.Rotation;
            Vector2 origin = (centeredOrigin ? new Vector2((image.Width + 2) / 2f, (image.Height + 2) / 2f) : (image.Origin - image.Texture.DrawOffset)) / scaleFix;
            Draw.SpriteBatch.Draw(outline, drawPos, null, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
        }
    }
}
