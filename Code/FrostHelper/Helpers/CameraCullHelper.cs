//#define CULL_RECT_RENDER

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
}