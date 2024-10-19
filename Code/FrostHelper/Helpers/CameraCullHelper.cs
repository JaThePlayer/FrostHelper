//#define CULL_RECT_RENDER

using FrostHelper.Helpers;

namespace FrostHelper; 
public static class CameraCullHelper {
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

    internal static bool IsLineVisible(Vector2 a, Vector2 b, float lenience = 4f, Camera? camera = null) {
        var rect = RectangleExt.FromPoints(a, b);
        return IsRectangleVisible(rect.X, rect.Y, rect.Width, rect.Height, lenience, camera);
    }

    /// <summary>
    /// Checks if the curve is visible by creating a rectangle containing the edge points of the curve (Begin, Curve, End)
    /// </summary>
    public static bool IsVisible(SimpleCurve curve, float heightIncrease = 0f, Camera? camera = null) {
        var a = curve.Begin;
        var b = curve.Control;
        var c = curve.End;

        var left   = Math.Min(a.X, Math.Min(b.X, c.X));
        var right  = Math.Max(a.X, Math.Max(b.X, c.X));
        var top    = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        var bottom = Math.Max(a.Y, Math.Max(b.Y, c.Y));

        return IsRectangleVisible(left, top, right - left, bottom - top + heightIncrease, camera: camera);
    }
    
    /// <summary>
    /// Checks whether the <paramref name="sprite"/> is visible, taking account Scale and Origin.
    /// TODO: Justify, Rotation
    /// </summary>
    public static bool IsVisible(Sprite sprite, Camera? cam = null) {
        var s = sprite.Scale;
        int w = (int)(sprite.Width * s.X);
        int h = (int)(sprite.Height * s.Y);
        var j = sprite.Origin;

        return IsRectangleVisible(
            new((int) sprite.RenderPosition.X - (int) j.X / 2, (int) sprite.RenderPosition.Y - (int) j.Y / 2, w, h),
            camera: cam);
    }

    public static Rectangle GetRectangle(this Image image) {
        var renderPos = image.RenderPosition;
        var scale = image.Scale;
        var size = new Vector2(image.Width, image.Height) * scale;
        if (image.Rotation == 0f) {
            Vector2 pos = renderPos - image.Origin * scale + image.Texture.DrawOffset;

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

        return RectangleExt.FromPoints(r1, r2);
    }

    /// <summary>
    /// Returns a rectangle which contains the region of the provided rectangle which is visible
    /// </summary>
    internal static Rectangle GetVisibleSection(Rectangle r, int lenience = 4, Camera? camera = null) {
        camera ??= (Engine.Scene as Level)?.Camera;

        if (camera is null)
            return r;

        var left = int.Max(r.X, (int) camera.Left - lenience);
        var top = int.Max(r.Y, (int) camera.Top - lenience);
        var right = int.Min(r.Right, (int)camera.Right + lenience);
        var bot = int.Min(r.Bottom, (int)camera.Bottom + lenience);

        return new(left, top, right - left, bot - top);
    }
}