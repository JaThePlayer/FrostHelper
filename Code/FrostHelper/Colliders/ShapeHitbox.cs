namespace FrostHelper.Colliders;

public class ShapeHitbox : Collider {
    static bool loaded;

    public static void Load() {
        if (!loaded) {
            loaded = true;
            using (DetourContext ctx = new() { Before = new() { "*" } }) {
                On.Monocle.Collider.Collide_Collider += Collider_Collide_Collider;

            }
        }
    }

    private static bool Collider_Collide_Collider(On.Monocle.Collider.orig_Collide_Collider orig, Collider self, Collider collider) {
        if (collider is ShapeHitbox shape) {
            if (self is Hitbox hitbox)
                return shape.Collide(hitbox);
            else if (self is Circle circle)
                return shape.Collide(circle);
            else
                throw new Exception($"Collider implementedn't: Shape <-> {self.GetType().FullName}");
        } else {
            return orig(self, collider);
        }
    }

    [OnUnload]
    public static void Unload() {
        if (loaded) {
            Logger.Log(LogLevel.Warn, "FrostHelper.ShapeHitbox", "Not unloading the Collider.Collide(Collider) hook, as they were used this session!");
            return;
        }
        //On.Monocle.Collider.Collide_Collider -= Collider_Collide_Collider;
    }

    public Vector2[] Points;

    public bool Fill = true;

    public Rectangle Rectangle => new Rectangle((int) Left, (int) Top, (int) Width, (int) Height);

    public override float Width {
        get => Math.Abs(Right - Left);
        set => throw new NotImplementedException();
    }

    public override float Height {
        get => Math.Abs(Bottom - Top);
        set => throw new NotImplementedException();
    }

    public override float Top {
        get => Points.Min(v => v.Y);
        set => throw new NotImplementedException();
    }

    public override float Bottom {
        get => Points.Max(v => v.Y);
        set => throw new NotImplementedException();
    }

    public override float Left {
        get => Points.Min(v => v.X);
        set => throw new NotImplementedException();
    }
    public override float Right {
        get => Points.Max(v => v.X);
        set => throw new NotImplementedException();
    }

    public ShapeHitbox(Vector2[] vectors) {
        Points = vectors;
        Load();
    }

    public override Collider Clone() {
        return new ShapeHitbox(Points);
    }

    public override bool Collide(Vector2 point) {
        throw new NotImplementedException();
    }

    public override bool Collide(Rectangle rect) {
        for (int i = 0; i < Points.Length - 1; i++) {
            if (Monocle.Collide.RectToLine(rect.Left, rect.Top, rect.Width, rect.Height, Points[i], Points[i + 1]))
                return true;
        }

        if (Fill && Monocle.Collide.RectToLine(rect.Left, rect.Top, rect.Width, rect.Height, Points[0], Points[Points.Length - 1]))
            return true;

        return false;
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
        throw new NotImplementedException();
    }

    public override void Render(Camera camera, Color color) {
        for (int i = 0; i < Points.Length - 1; i++) {
            Draw.Line(Points[i], Points[i + 1], color);
        }
        Draw.Line(Points[0], Points[Points.Length - 1], color);
    }
}
