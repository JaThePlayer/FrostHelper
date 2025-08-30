using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper;

/// <summary>
/// CrystalDebris, but optimised
/// - When inside a solid, it gets removed immediately
/// - Broad Phase Collision
/// - Batched outline rendering (different visuals when several debris overlap, but ~4x faster)
/// </summary>
[Pooled]
public class FastCrystalDebris : Entity {
    private readonly Image _image;

    private float _percent;

    private float _duration;

    private Vector2 _speed;

    private readonly Collision _collideH;

    private readonly Collision _collideV;

    private bool _bossShatter;

    private Rectangle _levelBounds;
    
    private readonly CollideExt.MoveFastData _moveFast;
    
    public FastCrystalDebris() : base(Vector2.Zero) {
        Depth = -9990;
        Collider = new Hitbox(2f, 2f, -1f, -1f);
        _collideH = OnCollideH;
        _collideV = OnCollideV;
        _image = new Image(GFX.Game["particles/shard"]);
        _image.CenterOrigin();
        //Add(image);
        
        Add(new BatchedOutlineImage(_image, Color.Black));
        _image.Entity = this;
        Visible = false;
        
        _moveFast = new(this);
    }
    
        /*
    public override void Render() 
    {
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
        */

    private void Init(Vector2 position, Color color, bool boss) {
        Position = position;
        _image.Color = color;
        _image.Scale = Vector2.One;
        _percent = 0f;
        _duration = 20f; // boss ? Calc.Random.Range(0.25f, 1f) : Calc.Random.Range(1f, 2f);
        _speed = Calc.AngleToVector(Calc.Random.NextAngle(), boss ? Calc.Random.Range(200, 240) : Calc.Random.Range(60, 160));
        _bossShatter = boss;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        _levelBounds = scene.ToLevel().Bounds;
        _levelBounds.Inflate(8, 8);

        if (CollideCheck<Solid>()) {
            RemoveSelf();
        }
    }

    public override void Update() {
        _speed.X = Calc.Clamp(_speed.X, -100000f, 100000f);
        _speed.Y = Calc.Clamp(_speed.Y, -100000f, 100000f);
        // Intentionally don't call base.Update for perf.
        // base.Update();
        var level = Scene.ToLevel();
        if (_percent > 1f || !_levelBounds.Contains(Position.ToNumerics())) {
            RemoveSelf();
            return;
        }

        _percent += Engine.DeltaTime / _duration;
        if (!_bossShatter) {
            _speed.X = Calc.Approach(_speed.X, 0f, Engine.DeltaTime * 20f);
            _speed.Y += 200f * Engine.DeltaTime;
        } else {
            _speed = _speed.SafeNormalize() * Calc.Approach(_speed.Length(), 0f, 300f * Engine.DeltaTime);
        }

        var speedLen = _speed.Length();
        if (speedLen > 0f) {
            _image.Rotation = _speed.Angle();
        }
        _image.Scale = Vector2.One * Calc.ClampedMap(_percent, 0.8f, 1f, 1f, 0f);
        _image.Scale.X *= Calc.ClampedMap(speedLen, 0f, 400f, 1f, 2f);
        _image.Scale.Y *= Calc.ClampedMap(speedLen, 0f, 400f, 1f, 0.2f);
        
        var preMovePos = Position;
        var hitbox = (Hitbox) Collider;
        var hitboxRect = hitbox.GetAbsRect();
        //hitboxRect.Inflate(1, 1);

        var move = _speed * Engine.DeltaTime;
        _moveFast.HitboxRect = hitboxRect;
        _moveFast.MoveBoth(move, _collideH, _collideV);
        
        if (Position == preMovePos && CollideCheck<Solid>()) {
            // The debris is stuck in a solid, remove it.
            RemoveSelf();
            return;
        }
        
        if (level.OnInterval(0.05f)) {
            level.ParticlesFG.Emit(PDust, Position);
        }
    }

    private void OnCollideH(CollisionData hit) {
        _speed.X *= -0.8f;
    }

    private void OnCollideV(CollisionData hit) {
        if (_bossShatter) {
            RemoveSelf();
            return;
        }
        if (Math.Sign(_speed.X) != 0) {
            _speed.X += Math.Sign(_speed.X) * 5;
        } else {
            _speed.X += Calc.Random.Choose(-1, 1) * 5;
        }
        _speed.Y *= -1.2f;
    }

    public static void Burst(Vector2 position, Color color, bool boss, int count = 1) {
        for (int i = 0; i < count; i++) {
            FastCrystalDebris crystalDebris = Engine.Pooler.Create<FastCrystalDebris>();
            Vector2 debrisPosition = position + new Vector2(Calc.Random.Range(-4, 4), Calc.Random.Range(-4, 4));
            crystalDebris.Init(debrisPosition, color, boss);
            Engine.Scene.Add(crystalDebris);
        }
    }

    public static ParticleType PDust => CrystalDebris.P_Dust;
}
