using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;

internal static class DrawExt {
    private static Texture2D? _pixelTexture;

    public static Texture2D Pixel {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (_pixelTexture is { } pixel)
                return pixel;

            _pixelTexture = new Texture2D(Draw.Pixel.Texture.Texture.GraphicsDevice, 1, 1);
            _pixelTexture.SetData([ Color.White ]);

            return _pixelTexture;
        }
    }
}