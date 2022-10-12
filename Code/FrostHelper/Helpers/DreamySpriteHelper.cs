namespace FrostHelper.Helpers;

public static class DreamySpriteHelper {
    public static void DrawDreamySprite(Sprite img, float speed = 2f, float maxOffset = 2f) {
        int h = 0;
        while (h < img.Height) {
            img.DrawSubrect(
                new((float) Math.Sin(Engine.Scene.TimeActive * speed + h * 0.4f) * maxOffset, h), 
                new(0, h, (int) img.Width, 1)
            );
            h++;
        }
    }

}
