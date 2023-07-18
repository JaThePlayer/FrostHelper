//#define CULL_RECT_RENDER

using FrostHelper.Helpers;

namespace FrostHelper; 
public static class CameraCullHelper {
    /// <summary>
    /// Checks whether the given rectangle is visible inside of the camera at a given camera position
    /// </summary>
    internal static bool IsRectVisible(Vector2 cam, Rectangle rect) {
        var x = rect.X;
        var y = rect.Y;

        var w = rect.Width;
        var h = rect.Height;

        var camX = cam.X;
        var camY = cam.Y;

        const float lenience = 4f;

        return x + w >= camX - lenience
            && x <= camX + 320f + lenience
            && y + h >= camY - lenience
            && y <= camY + 180f + lenience;
    }

    public static bool IsRectangleVisible(float x, float y, float w, float h, float lenience = 4f, Camera? camera = null) {
        camera ??= (Engine.Scene as Level)?.Camera;
        return camera is null || (
            x + w >= camera.Left - lenience 
         && x <= camera.Right + lenience 
         && y + h >= camera.Top - lenience 
         && y <= camera.Bottom + lenience
        );
    }

    public static bool IsRectangleVisible(Rectangle rectangle, float lenience = 4f, Camera? camera = null) {
        return IsRectangleVisible(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, lenience, camera);
    }

    /// <summary>
    /// Checks whether the rectangle represented by (x,y,w,h) is visible inside of the camera at a given camera position
    /// </summary>
    internal static bool IsRectVisible(Vector2 cam, float x, float y, float w, float h) {
        var camX = cam.X;
        var camY = cam.Y;

        const float lenience = 4f;

#if CULL_RECT_RENDER
        Draw.Rect(x, y, w, h, Color.Pink * 0.7f);
#endif

        return x + w >= camX - lenience
            && x <= camX + 320f + lenience
            && y + h >= camY - lenience
            && y <= camY + 180f + lenience;
    }

    /// <summary>
    /// Creates a rectangle that contains both of the provided points, then checks if that rectangle is visible
    /// </summary>
    internal static bool IsRectVisible(Vector2 cam, Vector2 pointA, Vector2 pointB) {
        var left = Math.Min(pointA.X, pointB.X);
        var top = Math.Min(pointA.Y, pointB.Y);
        var w = Math.Abs(pointA.X - pointB.X);
        var h = Math.Abs(pointA.Y - pointB.Y);

        return IsRectVisible(cam, left, top, w, h);
    }

    /// <summary>
    /// Checks if the curve is visible by creating a rectangle containing the edge points of the curve (Begin, Curve, End)
    /// </summary>
    public static bool IsVisible(Vector2 cam, SimpleCurve curve, float heightIncrease = 0f) {
        var a = curve.Begin;
        var b = curve.Control;
        var c = curve.End;

        var left   = Math.Min(a.X, Math.Min(b.X, c.X));
        var right  = Math.Max(a.X, Math.Max(b.X, c.X));
        var top    = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        var bottom = Math.Max(a.Y, Math.Max(b.Y, c.Y));

        return IsRectVisible(cam, left, top, right - left, bottom - top + heightIncrease);
    }

    /// <summary>
    /// Checks whether the <paramref name="sprite"/> is visible, taking account Scale and Origin.
    /// TODO: Justify, Rotation
    /// </summary>
    public static bool IsVisible(Vector2 cam, Sprite sprite) {
        var s = sprite.Scale;
        int w = (int)(sprite.Width * s.X);
        int h = (int)(sprite.Height * s.Y);
        var j = sprite.Origin;

        return IsRectVisible(cam, new((int)sprite.RenderPosition.X - (int)j.X / 2, (int) sprite.RenderPosition.Y - (int) j.Y / 2, w, h));
    }

    public static Rectangle GetRectangle(this Image image) {
        var renderPos = image.RenderPosition;
        var scale = image.Scale;
        var size = new Vector2(image.Width, image.Height) * scale;
        Vector2 pos;
        if (image.Rotation == 0f) {
            pos = renderPos - image.Origin * scale + image.Texture.DrawOffset;

            return new Rectangle((int) pos.X, (int) pos.Y, (int) size.X, (int) size.Y);
        }

        // rotate our points, by rotating the offset
        var off = -image.Origin;

        var p1 = off.Rotate(image.Rotation);
        var p2 = (off + new Vector2(size.X, 0)).Rotate(image.Rotation);
        var p3 = (off + new Vector2(0, size.Y)).Rotate(image.Rotation);
        var p4 = (off + size).Rotate(image.Rotation);

        var r1 = renderPos + new Vector2(
            Math.Min(p4.X, Math.Min(p3.X, Math.Min(p1.X, p2.X))),
            Math.Min(p4.Y, Math.Min(p3.Y, Math.Min(p1.Y, p2.Y)))
        ) + image.Texture.DrawOffset.Rotate(image.Rotation);
        var r2 = renderPos + new Vector2(
            Math.Max(p4.X, Math.Max(p3.X, Math.Max(p1.X, p2.X))),
            Math.Max(p4.Y, Math.Max(p3.Y, Math.Max(p1.Y, p2.Y)))
        ) + image.Texture.DrawOffset.Rotate(image.Rotation);

        return RectangleExt.FromPoints(r1.ToPoint(), r2.ToPoint());
    }
}