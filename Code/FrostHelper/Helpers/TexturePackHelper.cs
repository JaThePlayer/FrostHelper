using System.Runtime.InteropServices;

namespace FrostHelper.Helpers;

internal static class TexturePackHelper {
    /// <summary>
    /// Packs all textures in the given groups into a single texture, then gives back groups of new textures that point to that new texture,
    /// in the same order as provided.
    /// </summary>
    public static List<List<MTexture>> CreatePackedGroups(List<List<MTexture>> inputTextureGroups, string debugName,
        out VirtualTexture? virtTexture) {
        var allPacked = CreatePacked(inputTextureGroups.SelectMany(e => e).ToList(), debugName, out virtTexture);
        var allPackedSpan = CollectionsMarshal.AsSpan(allPacked);
        var i = 0;

        List<List<MTexture>> output = new(inputTextureGroups.Count); 
        foreach (var group in inputTextureGroups) {
            output.Add([.. allPackedSpan[i .. (i + group.Count)]]);
            i += group.Count;
        }

        return output;
    }
    
    /// <summary>
    /// Packs the input texture into a new texture, then returns a new list of mtextures in the same order, which points to a packed texture.
    /// Used for improving rendering performance.
    /// virtTexture is null if the textures are too big to be packed, or there's only 1 input texture
    /// </summary>
    public static List<MTexture> CreatePacked(List<MTexture> inputTextures, string debugName, out VirtualTexture? virtTexture) {
        virtTexture = null;

        // no point in packing 0 or 1 textures
        if (inputTextures.Count is 0 or 1)
            return inputTextures;
        
        var width = 0;
        var height = 0;
        
        // currently, all textures are simply packed horizontally
        // todo: make better use of vertical space
        foreach (var inputTexture in inputTextures) {
            width += inputTexture.Width;
            height = int.Max(height, inputTexture.Height);
        }

        // XNA has a texture size limit
        if (width > 4096 || height > 4096)
            return inputTextures;

        var gd = Engine.Graphics.GraphicsDevice;
        var packedTexture = new RenderTarget2D(gd, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        virtTexture = new VirtualTexture($"fh.autopacked.{debugName}", width, height, Color.Transparent) {
            Texture = packedTexture
        };
        var mTexture = new MTexture(virtTexture);
        var packedTextures = new List<MTexture>(inputTextures.Count);
        
        gd.SetRenderTarget(packedTexture);
        gd.Clear(Color.Transparent);
        Draw.SpriteBatch.Begin();
        
        var pos = Point.Zero;
        foreach (var t in inputTextures)
        {
            t.Draw(pos.ToVector2(), Vector2.Zero);
            
            var newTexture = new MTexture(mTexture, pos.X, pos.Y, t.Width, t.Height);
            packedTextures.Add(newTexture);
            pos.X += t.Width;
        }
        
        Draw.SpriteBatch.End();
        gd.SetRenderTarget(null);

        return packedTextures;
    }
}