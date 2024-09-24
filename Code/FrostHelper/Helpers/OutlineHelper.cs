namespace FrostHelper;

static class OutlineHelper {
    static Dictionary<(string, Rectangle?), Texture2D> cache = new();

    /// <summary>
    /// ONLY CALL IN RENDER!
    /// Cursed for now
    /// </summary>
    public static Texture2D Get(string path, Rectangle? clip, bool inRender = true, bool isEightWay = false) {
        if (cache.TryGetValue((path, clip), out Texture2D? value))
            return value;

        MTexture mTexture = GFX.Game[path];
        RenderTarget2D target = new(Engine.Graphics.GraphicsDevice, mTexture.Width + 2 + (int) mTexture.DrawOffset.X, mTexture.Height + 2 + (int) mTexture.DrawOffset.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        var prevTarget = Draw.SpriteBatch.GraphicsDevice.GetRenderTargets();
        if (inRender)
            Draw.SpriteBatch.End();
        Draw.SpriteBatch.Begin();
        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(target);
        Draw.SpriteBatch.GraphicsDevice.Clear(Color.Transparent);

        Texture2D texture = mTexture.Texture.Texture_Safe;
        Rectangle? clipRect = clip ?? mTexture.ClipRect;
        float scaleFix = mTexture.ScaleFix;
        Vector2 origin = Vector2.Zero;//(Vector2.Zero - mTexture.DrawOffset) / scaleFix;
        Vector2 drawPos = Vector2.One;
        Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitY, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
        Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitY, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
        Draw.SpriteBatch.Draw(texture, drawPos - Vector2.UnitX, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
        Draw.SpriteBatch.Draw(texture, drawPos + Vector2.UnitX, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
        if (isEightWay) {
            Draw.SpriteBatch.Draw(texture, drawPos - Vector2.One, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.Draw(texture, drawPos + Vector2.One, clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.Draw(texture, drawPos + new Vector2(-1f, 1f), clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
            Draw.SpriteBatch.Draw(texture, drawPos + new Vector2(1f, -1f), clipRect, Color.White, 0f, origin, scaleFix, SpriteEffects.None, 0f);
        }
        Draw.SpriteBatch.End();

        if (inRender) {
            GameplayRenderer.Begin();
        }
        Draw.SpriteBatch.GraphicsDevice.SetRenderTargets(prevTarget);

        cache.Add((path, clip), target);
        return target;
    }

    public static Texture2D Get(Image image, bool inRender = true, bool isEightWay = false) {
        return Get(image.Texture.AtlasPath ?? image.Texture.Parent.AtlasPath, image.Texture.ClipRect, inRender, isEightWay);
    }

    public static void Dispose() {
        foreach (var item in cache) {
            item.Value.Dispose();
        }
        cache = new();
    }

    public static void RenderOutline(Image image, Color color, bool isEightWay = false) {
        var outline = Get(image, isEightWay: isEightWay);
        float scaleFix = image.Texture.ScaleFix;
        Vector2 drawPos = image.RenderPosition;// - new Vector2(1f, 1f).Rotate(image.Rotation);
        float rotation = image.Rotation;
        Vector2 origin = (image.Origin + Vector2.One - image.Texture.DrawOffset) / scaleFix;
        
        Draw.SpriteBatch.Draw(outline, drawPos, null, color, rotation, origin, scaleFix, SpriteEffects.None, 0f);
    }
}
