using FrostHelper.Helpers;
using System.Runtime.CompilerServices;

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

internal sealed class AnimatedImage : Image {
    public AnimatedMTexture? AnimatedTexture;

    public float Offset;
    public float Speed;

    private float _initialOffset;

    public void SetImage(AnimatedMTexture? newImg) {
        AnimatedTexture = newImg;
        Speed = newImg?.Speed ?? 0;
    }

    public void ResetAnimation(float time) {
        Offset = (-time * Speed) + _initialOffset;
    }
    
    public void ResetAndFinishIn(float time, float timeToFinishIn) {
        Speed = (AnimatedTexture?.Textures.Count ?? 1) / timeToFinishIn;
        ResetAnimation(time);
    }
    
    public AnimatedImage(AnimatedMTexture texture) : base(texture) {
        AnimatedTexture = texture;
        Offset = texture.CreateRandomAnimOffset();
        _initialOffset = Offset;
        Speed = texture.Speed;
    }
    
    public AnimatedImage(AnimatedMTexture texture, float offset) : base(texture) {
        AnimatedTexture = texture;
        Offset = offset;
        _initialOffset = offset;
        Speed = texture.Speed;
    }

    public static Image CreateAnimatedOrNot(MTexture texture) {
        return texture is AnimatedMTexture anim ? new AnimatedImage(anim) : new SealedImage(texture);
    }

    public new AnimatedImage JustifyOrigin(Vector2 vec) {
        base.JustifyOrigin(vec);
        return this;
    }

    public new AnimatedImage SetOrigin(float x, float y) {
        base.SetOrigin(x, y);
        return this;
    }
    
    public new AnimatedImage CenterOrigin() {
        base.CenterOrigin();
        return this;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MTexture GetTexture(float time) => AnimatedTexture?.GetAnim(time, Offset, Speed) ?? Texture;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAnimFrame(float time) => AnimatedTexture?.GetAnimFrame(time, Offset, Speed) ?? 0;
    
    public override void Render()
    {
        Render(Texture, Color);
    }

    public void RenderAnimated(float time) {
        Render(GetTexture(time), Color);
    }
    
    public void RenderAnimated(Color color, float time) {
        Render(GetTexture(time), color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Render(MTexture texture, Color color) {
        float scaleFix = texture.ScaleFix;
        Draw.SpriteBatch.Draw(texture.Texture.Texture_Safe, RenderPosition, texture.ClipRect, color, Rotation, 
            (Origin - texture.DrawOffset) / scaleFix, Scale * scaleFix, Effects, 0.0f);
    }
}

internal static class ImageExtensions {
    public static void RenderWithColor(this Image image, Color color) {
        var prev = image.Color;
        image.Color = color;
        image.Render();
        image.Color = prev;
    }

    public static MTexture GetTextureMaybeAnimated(this Image image, float time) {
        if (image is AnimatedImage anim)
            return anim.GetTexture(time);
        return image.Texture;
    }
    
    public static void RenderMaybeAnimated(this Image image, float time)
    {
        if (image is AnimatedImage anim)
            anim.RenderAnimated(time);
        else
            image.Render();
    }
    
    public static void RenderMaybeAnimated(this Image image, Color color, float time)
    {
        if (image is AnimatedImage anim) {
            anim.RenderAnimated(color, time);
            return;
        }
        
        var texture = image.Texture;
        float scaleFix = texture.ScaleFix;
        Draw.SpriteBatch.Draw(texture.Texture.Texture_Safe, image.RenderPosition, texture.ClipRect, color, image.Rotation, 
            (image.Origin - texture.DrawOffset) / scaleFix, image.Scale * scaleFix, image.Effects, 0.0f);
    }

    public static float CreateRandomAnimOffset(this MTexture img) {
        if (img is AnimatedMTexture anim) {
            var meta = anim.Meta;
            return meta.RandomizeStartFrame ? Calc.Random.NextFloat(anim.Textures.Count) : 0f;
        }

        return 0f;
    }
}