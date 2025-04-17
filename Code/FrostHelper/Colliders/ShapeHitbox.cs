using FrostHelper.Helpers;

namespace FrostHelper.Colliders;

public sealed class ShapeHitbox : ColliderList {
    public readonly Vector2[] Points;

    public bool Fill = true;

    private Rectangle _cullRectangle;

    private Rectangle GetCullRectangle() => _cullRectangle.MovedBy(Entity?.Position ?? default);
    
    private float _width, _height, _top, _left, _right, _bottom;

    public override float Width {
        get => _width;
        set => throw new NotImplementedException();
    }

    public override float Height {
        get => _height;
        set => throw new NotImplementedException();
    }

    public override float Top {
        get => _top - (Entity?.Y ?? default);
        set => throw new NotImplementedException();
    }

    public override float Bottom {
        get => _bottom - (Entity?.Y ?? default);
        set => throw new NotImplementedException();
    }

    public override float Left {
        get => _left - (Entity?.X ?? default);
        set => throw new NotImplementedException();
    }
    public override float Right {
        get => _right - (Entity?.X ?? default);
        set => throw new NotImplementedException();
    }

    public ShapeHitbox(Vector2[] vectors) {
        Points = vectors;
        colliders = [];

        OnChanged();
    }

    public void OnChanged() {
        _bottom = Points.Max(v => v.Y);
        _top = Points.Min(v => v.Y);
        _left = Points.Min(v => v.X);
        _right = Points.Max(v => v.X);
        _width = Math.Abs(_right - _left);
        _height = Math.Abs(_bottom - _top);
        
        _cullRectangle = new Rectangle((int) _left - 1, (int) _top - 1, (int) _width + 2, (int) _height + 2);
    }

    public override void Added(Entity entity) {
        base.Added(entity);
        var by = entity.Position;
        var points = Points;
        for (int i = 0; i < points.Length; i++)
            points[i] -= by;
        
        OnChanged();
    }

    public override Collider Clone() {
        return new ShapeHitbox(Points);
    }

    public override bool Collide(Vector2 point) {
        return Collide(new Rectangle((int)point.X, (int)point.Y, 1, 1));
    }

    public override bool Collide(Rectangle rect) {
        if (!GetCullRectangle().Intersects(rect))
            return false;

        var pos = Entity?.Position ?? default;
        var points = Points;
        for (int i = 0; i < points.Length - 1; i++) {
            if (Monocle.Collide.RectToLine(rect.Left, rect.Top, rect.Width, rect.Height, points[i]+pos, points[i + 1]+pos))
                return true;
        }

        return Fill && Monocle.Collide.RectToLine(rect.Left, rect.Top, rect.Width, rect.Height, points[0]+pos, points[^1]+pos);
    }

    public override bool Collide(Vector2 from, Vector2 to) {
        throw new NotImplementedException();
    }

    public override bool Collide(Hitbox hitbox) {
        return Collide(new Rectangle((int) hitbox.AbsoluteX, (int) hitbox.AbsoluteY, (int) hitbox.Width, (int) hitbox.Height));
    }

    public override bool Collide(Grid grid) {
        throw new NotImplementedException();
    }

    public override bool Collide(Circle circle) {
        throw new NotImplementedException();
    }

    public override bool Collide(ColliderList list) {
        foreach (Collider collider in list.colliders)
            if (collider.Collide(this))
                return true;
        return false;
    }

    public override void Render(Camera camera, Color color) {
        if (!CameraCullHelper.IsRectangleVisible(GetCullRectangle(), camera: camera))
            return;
        
        var pos = Entity?.Position ?? default;
        var points = Points;
        for (int i = 0; i < points.Length - 1; i++) {
            Draw.Line(points[i]+pos, points[i + 1]+pos, color);
        }
        Draw.Line(points[0]+pos, points[^1]+pos, color);
    }
}
