namespace FrostHelper;

/// <summary>
/// CrystalDebris, but optimised
/// - When inside a solid, it gets removed immediately
/// </summary>
[Pooled]
public class FastCrystalDebris : Actor {
    public FastCrystalDebris() : base(Vector2.Zero) {
        Depth = -9990;
        Collider = new Hitbox(2f, 2f, -1f, -1f);
        collideH = OnCollideH;
        collideV = OnCollideV;
        image = new Image(GFX.Game["particles/shard"]);
        image.CenterOrigin();
        Add(image);
    }

    private void Init(Vector2 position, Color color, bool boss) {
        Position = position;
        image.Color = color;
        image.Scale = Vector2.One;
        percent = 0f;
        duration = boss ? Calc.Random.Range(0.25f, 1f) : Calc.Random.Range(1f, 2f);
        speed = Calc.AngleToVector(Calc.Random.NextAngle(), boss ? Calc.Random.Range(200, 240) : Calc.Random.Range(60, 160));
        bossShatter = boss;
    }

    public override void Update() {
        speed.X = Calc.Clamp(speed.X, -100000f, 100000f);
        speed.Y = Calc.Clamp(speed.Y, -100000f, 100000f);
        base.Update();
        if (percent > 1f || CollideCheck<Solid>()) {
            RemoveSelf();
            return;
        }
        percent += Engine.DeltaTime / duration;
        if (!bossShatter) {
            speed.X = Calc.Approach(speed.X, 0f, Engine.DeltaTime * 20f);
            speed.Y += 200f * Engine.DeltaTime;
        } else {
            speed = speed.SafeNormalize() * Calc.Approach(speed.Length(), 0f, 300f * Engine.DeltaTime);
        }

        var speedLen = speed.Length();
        if (speedLen > 0f) {
            image.Rotation = speed.Angle();
        }
        image.Scale = Vector2.One * Calc.ClampedMap(percent, 0.8f, 1f, 1f, 0f);
        image.Scale.X *= Calc.ClampedMap(speedLen, 0f, 400f, 1f, 2f);
        image.Scale.Y *= Calc.ClampedMap(speedLen, 0f, 400f, 1f, 0.2f);
        MoveH(speed.X * Engine.DeltaTime, collideH, null);
        MoveV(speed.Y * Engine.DeltaTime, collideV, null);
        if (Scene.OnInterval(0.05f)) {
            (Scene as Level)!.ParticlesFG.Emit(P_Dust, Position);
        }
    }

    public override void Render() {
        Color color = image.Color;
        image.Color = Color.Black;
        image.Position = new Vector2(-1f, 0f);
        image.Render();
        image.Position = new Vector2(0f, -1f);
        image.Render();
        image.Position = new Vector2(1f, 0f);
        image.Render();
        image.Position = new Vector2(0f, 1f);
        image.Render();
        image.Position = Vector2.Zero;
        image.Color = color;
        base.Render();
    }

    private void OnCollideH(CollisionData hit) {
        speed.X *= -0.8f;
    }

    private void OnCollideV(CollisionData hit) {
        if (bossShatter) {
            RemoveSelf();
            return;
        }
        if (Math.Sign(speed.X) != 0) {
            speed.X += Math.Sign(speed.X) * 5;
        } else {
            speed.X += Calc.Random.Choose(-1, 1) * 5;
        }
        speed.Y *= -1.2f;
    }

    public static void Burst(Vector2 position, Color color, bool boss, int count = 1) {
        for (int i = 0; i < count; i++) {
            FastCrystalDebris crystalDebris = Engine.Pooler.Create<FastCrystalDebris>();
            Vector2 debrisPosition = position + new Vector2(Calc.Random.Range(-4, 4), Calc.Random.Range(-4, 4));
            crystalDebris.Init(debrisPosition, color, boss);
            Engine.Scene.Add(crystalDebris);
        }
    }

    public static ParticleType P_Dust => CrystalDebris.P_Dust;

    private Image image;

    private float percent;

    private float duration;

    private Vector2 speed;

    private Collision collideH;

    private Collision collideV;

    private bool bossShatter;
}
