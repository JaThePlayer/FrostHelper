namespace FrostHelper.Helpers;

internal readonly struct FastDraw {
    public FastDraw() {
        var pixel = Draw.Pixel;
        _pixelTexture2D = pixel.Texture.Texture_Safe;
        var clipRect = Draw.Pixel.ClipRect;
        //_pixelClipRect = clipRect;
        
        _pixelSourceX = clipRect.X / (float) _pixelTexture2D.Width;
        _pixelSourceY = clipRect.Y / (float) _pixelTexture2D.Height;
        _pixelSourceW = clipRect.Width / (float) _pixelTexture2D.Width;
        _pixelSourceH = clipRect.Height / (float) _pixelTexture2D.Height;
    }

    private readonly Texture2D _pixelTexture2D;
    //private readonly Rectangle? _pixelClipRect;
    private readonly float _pixelSourceX, _pixelSourceY, _pixelSourceW, _pixelSourceH;
    
    public void Rect(float x, float y, float width, float height, Color color)
    {
        //Draw.SpriteBatch.Draw(_pixelTexture2D, new Rectangle((int)x, (int)y, (int)width, (int)height), _pixelClipRect, color);
        Draw.SpriteBatch.PushSprite(_pixelTexture2D, 
            _pixelSourceX, _pixelSourceY, _pixelSourceW, _pixelSourceH,
            x, y, width, height,
            color, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0);
    }
    
    public void Rect(Vector2 position, float width, float height, Color color) {
        //Draw.SpriteBatch.Draw(_pixelTexture2D, new Rectangle((int)position.X, (int)position.Y, (int)width, (int)height), _pixelClipRect, color);
        Draw.SpriteBatch.PushSprite(_pixelTexture2D, 
            _pixelSourceX, _pixelSourceY, _pixelSourceW, _pixelSourceH,
            (int)position.X, (int)position.Y, width, height,
            color, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0);
    }
}