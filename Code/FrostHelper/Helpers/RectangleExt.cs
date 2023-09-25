namespace FrostHelper.Helpers;

public static class RectangleExt {
    public static Rectangle FromPoints(Vector2 a, Vector2 b) => FromPoints(a.ToPoint(), b.ToPoint());

    //https://stackoverflow.com/questions/45259380/convert-2-vector2-points-to-a-rectangle-in-xna-monogame
    public static Rectangle FromPoints(Point a, Point b) {
        //we need to figure out the top left and bottom right coordinates
        //we need to account for the fact that a and b could be any two opposite points of a rectangle, not always coming into this method as topleft and bottomright already.
        int smallestX = Math.Min(a.X, b.X);
        int smallestY = Math.Min(a.Y, b.Y);
        int largestX = Math.Max(a.X, b.X);
        int largestY = Math.Max(a.Y, b.Y);

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }

    public static Rectangle FromPoints(Vector2[] points) {
        int smallestX = (int) points.Min(v => v.X);
        int smallestY = (int) points.Min(v => v.Y);
        int largestX = (int) points.Max(v => v.X);
        int largestY = (int) points.Max(v => v.Y);

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }

    public static Rectangle FromPoints(Vector3[] points) {
        int smallestX = (int) points.Min(v => v.X);
        int smallestY = (int) points.Min(v => v.Y);
        int largestX = (int) points.Max(v => v.X);
        int largestY = (int) points.Max(v => v.Y);

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }

    public static Rectangle Merge(Rectangle a, Rectangle b) {
        int smallestX = Math.Min(a.Left, b.Left);
        int smallestY = Math.Min(a.Top, b.Top);
        int largestX = Math.Max(a.Right, b.Right);
        int largestY = Math.Max(a.Bottom, b.Bottom);

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }

    public static Rectangle Merge(IEnumerable<Rectangle> rectangles) {
        bool any = false;
        int smallestX = int.MaxValue;
        int smallestY = int.MaxValue;
        int largestX = int.MinValue;
        int largestY = int.MinValue;

        foreach (var r in rectangles) {
            any = true;

            smallestX = Math.Min(smallestX, r.X);
            smallestY = Math.Min(smallestY, r.Y);
            largestX = Math.Max(largestX, r.Right);
            largestY = Math.Max(largestY, r.Bottom);
        }

        if (!any) {
            return new Rectangle(0, 0, 0, 0);
        }

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }

    public static Rectangle MultSize(this Rectangle r, int mult) {
        return new(r.X, r.Y, r.Width * mult, r.Height * mult);
    }

    public static Rectangle Mult(this Rectangle r, int mult) {
        return new(r.X * mult, r.Y * mult, r.Width * mult, r.Height * mult);
    }

    public static Rectangle Div(this Rectangle r, int mult) {
        return new(r.X / mult, r.Y / mult, r.Width / mult, r.Height / mult);
    }

    public static Point Size(this Rectangle r) => new(r.Width, r.Height);

    public static Rectangle AddSize(this Rectangle r, int w, int h) => new(r.X, r.Y, r.Width + w, r.Height + h);
    public static Rectangle AddSize(this Rectangle r, Point offset) => new(r.X, r.Y, r.Width + offset.X, r.Height + +offset.Y);

    public static Rectangle MovedBy(this Rectangle r, Vector2 offset) => new(r.X + (int) offset.X, r.Y + (int) offset.Y, r.Width, r.Height);
    public static Rectangle MovedBy(this Rectangle r, int x, int y) => new(r.X + x, r.Y + y, r.Width, r.Height);
    public static Rectangle MovedTo(this Rectangle r, Vector2 pos) => new((int) pos.X, (int) pos.Y, r.Width, r.Height);
}
