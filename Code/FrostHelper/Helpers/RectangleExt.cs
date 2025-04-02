using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;

public static class RectangleExt {
    public static Rectangle FromPoints(Vector2 a, Vector2 b)
        => FromTwoPointsCore<Vector2, GetX, GetY>(a, b);

    internal static Rectangle FromPoints(NumVector2 a, NumVector2 b)
        => FromTwoPointsCore<NumVector2, GetX, GetY>(a, b);
    
    public static Rectangle FromPoints(Point a, Point b)
        => FromTwoPointsCore<Point, GetX, GetY>(a, b);

    public static Rectangle FromPoints(IEnumerable<Vector2> points)
        => FromPointsCore<Vector2, IEnumerator<Vector2>, GetX, GetY>(points.GetEnumerator());

    /// <summary>
    /// Creates a rectangle out of X and Y coordinates of the provided points.
    /// </summary>
    public static Rectangle FromPointsFromXY(Vector3[] points)
        => FromPointsCore<Vector3, LinqExt.ArrayEnumerator<Vector3>, GetX, GetY>(points.GetArrayEnumerator());

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
        int smallestX = int.MaxValue, smallestY = int.MaxValue;
        int largestX = int.MinValue, largestY = int.MinValue;

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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Rectangle FromPointsCore<T, TEnumerator, TGetX, TGetY>(TEnumerator points)
    where TGetX : struct, IStaticFunc<T, int>
    where TGetY : struct, IStaticFunc<T, int>
    where TEnumerator : IEnumerator<T> {
        int smallestX = int.MaxValue, smallestY = int.MaxValue;
        int largestX = int.MinValue, largestY = int.MinValue;
        
        while (points.MoveNext()) {
            var p = points.Current;

            var x = TGetX.Invoke(p);
            if (x < smallestX) {
                smallestX = x;
            } else if (x > largestX) {
                largestX = x;
            }
            
            var y = TGetY.Invoke(p);
            if (y < smallestY) {
                smallestY = y;
            } else if (y > largestY) {
                largestY = y;
            }
        }

        return new Rectangle(smallestX, smallestY, largestX - smallestX, largestY - smallestY);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Rectangle FromTwoPointsCore<T, TGetX, TGetY>(T a, T b)
        where TGetX : struct, IStaticFunc<T, int>
        where TGetY : struct, IStaticFunc<T, int> {
        var ax = TGetX.Invoke(a);
        var ay = TGetY.Invoke(a);
        var bx = TGetX.Invoke(b);
        var by = TGetY.Invoke(b);
            
        if (ax > bx)
            (ax, bx) = (bx, ax);
        if (ay > by)
            (ay, by) = (by, ay);

        return new Rectangle(ax, ay, bx - ax, by - ay);
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
    
    private struct GetX : IStaticFunc<Vector2, int>, IStaticFunc<Vector3, int>, IStaticFunc<Point, int>, IStaticFunc<NumVector2, int> {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Invoke(Vector2 arg) => (int)arg.X;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Invoke(NumVector2 arg) => (int)arg.X;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Invoke(Vector3 arg) => (int)arg.X;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Invoke(Point arg) => arg.X;
    }
    
    private struct GetY : IStaticFunc<Vector2, int>, IStaticFunc<Vector3, int>, IStaticFunc<Point, int>, IStaticFunc<NumVector2, int> {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Invoke(Vector2 arg) => (int)arg.Y;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Invoke(NumVector2 arg) => (int)arg.Y;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Invoke(Vector3 arg) => (int)arg.Y;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Invoke(Point arg) => arg.Y;
    }
}
