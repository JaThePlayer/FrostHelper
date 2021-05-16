using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Collections.Generic;

namespace FrostHelper
{
    static class OutlineHelper
    {
        static Dictionary<string, RenderTarget2D> cache = new Dictionary<string, RenderTarget2D>();

        /// <summary>
        /// ONLY CALL IN RENDER!
        /// Cursed for now
        /// </summary>
        public static RenderTarget2D Get(string path, bool inRender = true)
        {
            if (cache.ContainsKey(path))
                return cache[path];

            MTexture mTexture = GFX.Game[path];
            RenderTarget2D target = new RenderTarget2D(Engine.Graphics.GraphicsDevice, mTexture.Width + 2 + (int)mTexture.DrawOffset.X, mTexture.Height+2 + (int)mTexture.DrawOffset.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            var prevTarget = Draw.SpriteBatch.GraphicsDevice.GetRenderTargets();

            if (inRender)
                Draw.SpriteBatch.End();
            Draw.SpriteBatch.Begin();
            Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(target);

            Texture2D texture = mTexture.Texture.Texture_Safe;
            Rectangle? clipRect = new Rectangle?(mTexture.ClipRect);
            float scaleFix = mTexture.ScaleFix;
            Vector2 origin = (Vector2.Zero - mTexture.DrawOffset) / scaleFix;
            Vector2 drawPos = Vector2.One;
            Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitY, clipRect, Color.Black, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitY, clipRect, Color.Black, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitX, clipRect, Color.Black, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitX, clipRect, Color.Black, 0f, origin, scaleFix, SpriteEffects.None, 0f);

            Draw.SpriteBatch.End();
            if (inRender)
            {
                GameplayRenderer.Begin();
            }
            Draw.SpriteBatch.GraphicsDevice.SetRenderTargets(prevTarget);

            cache.Add(path, target);
            return target;
        }

        public static void RenderOutline(Image image)
        {
            var outline = Get(image.Texture.AtlasPath ?? image.Texture.Parent.AtlasPath);
            float scaleFix = image.Texture.ScaleFix;
            Vector2 drawPos = image.RenderPosition - image.Texture.DrawOffset - new Vector2(1f, 1f).Rotate(image.Rotation);
            float rotation = image.Rotation;
            Vector2 origin = (image.Origin - image.Texture.DrawOffset) / scaleFix;
            Draw.SpriteBatch.Draw(outline, drawPos, null, Color.Black, rotation, origin, scaleFix, SpriteEffects.None, 0f);
        }
    }
}
