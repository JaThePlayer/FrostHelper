using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostHelper
{
    static class OutlineHelper
    {
        static Dictionary<string, RenderTarget2D> cache = new Dictionary<string, RenderTarget2D>();

        /// <summary>
        /// ONLY CALL IN RENDER!
        /// Cursed for now
        /// </summary>
        public static RenderTarget2D Get(string path)
        {
            if (cache.ContainsKey(path))
                return cache[path];

            MTexture mTexture = GFX.Game[path];
            RenderTarget2D target = new RenderTarget2D(Engine.Graphics.GraphicsDevice, mTexture.Width+2, mTexture.Height+2, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            var prevTarget = Draw.SpriteBatch.GraphicsDevice.GetRenderTargets();
            Draw.SpriteBatch.End();
            GameplayRenderer.Begin();
            Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(target);
            
            Vector2 basePos = Vector2.One;
            mTexture.Draw(basePos + new Vector2(-1f, -1f), Vector2.Zero, Color.White);
            mTexture.Draw(basePos + new Vector2(-1f, 1f), Vector2.Zero, Color.White);
            mTexture.Draw(basePos + new Vector2(1f, -1f), Vector2.Zero, Color.White);
            mTexture.Draw(basePos + new Vector2(1f, 1f), Vector2.Zero, Color.White);

            Draw.SpriteBatch.End();
            GameplayRenderer.Begin();
            Draw.SpriteBatch.GraphicsDevice.SetRenderTargets(prevTarget);
            
            cache.Add(path, target);
            return target;
        }
    }
}
