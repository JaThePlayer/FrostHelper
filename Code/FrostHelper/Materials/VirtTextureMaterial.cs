namespace FrostHelper.Materials;

internal sealed class VirtTextureMaterial(VirtualTexture texture, bool disposeTextureOnDispose) : IMaterial {
    private VirtualTexture? _texture = texture;
    
    public Texture2D GetTexture() {
        return _texture?.Texture ?? throw new ObjectDisposedException(nameof(VirtTextureMaterial));
    }

    public void Dispose() {
        if (disposeTextureOnDispose) {
            var texture = _texture;
            _texture = null;
            texture?.Dispose();
        }
    }

    public void Fill(Rectangle bounds, in RenderContext ctx) {
        var b = Draw.SpriteBatch;
        
        b.Draw(GetTexture(), bounds, Color.White);
    }

    public void Fill(RenderTarget2D target, in RenderContext ctx) {
        var b = Draw.SpriteBatch;
        
        b.Draw(GetTexture(), new Rectangle(0, 0, target.Width, target.Height), Color.White);
    }
}
