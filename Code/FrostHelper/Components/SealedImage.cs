namespace FrostHelper.Components;

/// <summary>
/// Same as Image, but sealed for perf
/// </summary>
internal sealed class SealedImage : Image {
    public SealedImage(MTexture texture) : base(texture)
    {
    }

    public SealedImage(MTexture texture, bool active) : base(texture, active)
    {
    }

    public new SealedImage JustifyOrigin(Vector2 vec) {
        base.JustifyOrigin(vec);
        return this;
    }

    public new SealedImage SetOrigin(float x, float y) {
        base.SetOrigin(x, y);
        return this;
    }
    
    public new SealedImage CenterOrigin() {
        base.CenterOrigin();
        return this;
    }
    
    public override void Render()
    {
        float scaleFix = Texture.ScaleFix;
        Draw.SpriteBatch.Draw(Texture.Texture.Texture_Safe, RenderPosition, Texture.ClipRect, Color, Rotation, 
            (Origin - Texture.DrawOffset) / scaleFix, Scale * scaleFix, Effects, 0.0f);
    }
    
    public void RenderWithColor(Color color)
    {
        float scaleFix = Texture.ScaleFix;
        Draw.SpriteBatch.Draw(Texture.Texture.Texture_Safe, RenderPosition, Texture.ClipRect, color, Rotation, 
            (Origin - Texture.DrawOffset) / scaleFix, Scale * scaleFix, Effects, 0.0f);
    }
}