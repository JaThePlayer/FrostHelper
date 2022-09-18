namespace FrostHelper;

public class CustomSnowball : Entity {
    public float Speed;
    public float ResetTime;
    public bool DrawOutline;
    public AppearDirection appearDirection;
    public float SafeZoneSize;

    private bool leaving;

    public CustomSnowball(string spritePath = "snowball", float speed = 200f, float resetTime = 0.8f, 
                          float sineWaveFrequency = 0.5f, bool drawOutline = true,
                          AppearDirection dir = AppearDirection.Right,
                          float safeZoneSize = 64f) {
        appearDirection = dir;

        Speed = speed;
        if (dir == AppearDirection.Left || dir == AppearDirection.Top)
            Speed = -speed;

        ResetTime = resetTime;
        DrawOutline = drawOutline;
        Depth = -12500;

        Collider = new Hitbox(12f, 9f, -5f, -2f);
        bounceCollider = new Hitbox(16f, 6f, -6f, -8f);

        Add(new PlayerCollider(OnPlayer, null, null));
        Add(new PlayerCollider(OnPlayerBounce, bounceCollider, null));
        Add(Sine = new SineWave(sineWaveFrequency, 0f));

        CreateSprite(spritePath);

        Sprite!.Play("spin", false, false);
        Add(spawnSfx = new SoundSource());
        SafeZoneSize = safeZoneSize;
    }

    public void StartLeaving() {
        leaving = true;
    }

    public void CreateSprite(string path) {
        Sprite?.RemoveSelf();
        Add(Sprite = GFX.SpriteBank.Create(path));

        if (IsVertical()) {
        //    Sprite.Rotation = Calc.ToRad(90f);
        }
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        level = SceneAs<Level>();
        ResetPosition();
    }

    private void ResetPosition() {
        Player player = level.Tracker.GetEntity<Player>();
        if (player != null && CheckIfPlayerOutOfBounds(player)) {
            spawnSfx.Play("event:/game/04_cliffside/snowball_spawn", null, 0f);
            Collidable = Visible = true;
            resetTimer = 0f;

            if (IsVertical()) {
                Y = GetResetXPosition();
                atY = X = player.CenterX;
            } else {
                X = GetResetXPosition();
                atY = Y = player.CenterY;
            }

            Sine.Reset();
            Sprite.Play("spin", false, false);
            return;
        }
        resetTimer = 0.05f;
    }

    private bool IsVertical() => appearDirection is AppearDirection.Top or AppearDirection.Bottom;

    /// <summary>
    /// If false, the snowball should not respawn, so as to not kill the player unfairly
    /// </summary>
    private bool CheckIfPlayerOutOfBounds(Player player) {
        if (player is null)
            return false;

        return appearDirection switch {
            AppearDirection.Right => player.Right < (level.Bounds.Right - SafeZoneSize),
            AppearDirection.Left => player.Left > (level.Bounds.Left + SafeZoneSize),
            AppearDirection.Top => player.Top > (level.Bounds.Top + SafeZoneSize),
            AppearDirection.Bottom => (player.Bottom < (level.Bounds.Bottom - SafeZoneSize)),
            _ => throw new NotImplementedException($"Unknown direction for snowballs: {appearDirection}"),
        };
    }

    private float GetResetXPosition() {
        return appearDirection switch {
            AppearDirection.Right => level.Camera.Right + 10f,
            AppearDirection.Left => level.Camera.Left - 10f,
            AppearDirection.Top => level.Camera.Top - 10f,
            AppearDirection.Bottom => level.Camera.Bottom + 10f,
            _ => throw new NotImplementedException($"Unknown direction for snowballs: {appearDirection}"),
        };
    }

    private bool IsOutOfBounds() {
        return appearDirection switch {
            AppearDirection.Right => X < level.Camera.Left - 60f,
            AppearDirection.Left => X > level.Camera.Right + 60f,
            AppearDirection.Top => Y > level.Camera.Bottom + 60f,
            AppearDirection.Bottom => Y < level.Camera.Top - 60f,
            _ => throw new NotImplementedException(),
        };
    }

    private void Destroy() {
        Collidable = false;
        Sprite.Play("break", false, false);
    }

    private void OnPlayer(Player player) {
        player.Die(new Vector2(-1f, 0f), false, true);
        Destroy();
        Audio.Play("event:/game/04_cliffside/snowball_impact", Position);
    }

    private void OnPlayerBounce(Player player) {
        if (!CollideCheck(player)) {
            Celeste.Celeste.Freeze(0.1f);
            player.Bounce(Top - 2f);
            Destroy();
            Audio.Play("event:/game/general/thing_booped", Position);
        }
    }

    public override void Update() {
        base.Update();

        if (IsVertical()) {
            Y -= Speed * Engine.DeltaTime;
            X = atY + 4f * Sine.Value;
        } else {
            X -= Speed * Engine.DeltaTime;
            Y = atY + 4f * Sine.Value;
        }

        if (IsOutOfBounds()) {
            if (leaving) {
                RemoveSelf();
                return;
            }
            resetTimer += Engine.DeltaTime;
            if (resetTimer >= ResetTime) {
                ResetPosition();
            }
        }
    }

    public override void Render() {
        if (DrawOutline)
            Sprite.DrawOutline(1);
        base.Render();
    }

    public Sprite Sprite;
    private float resetTimer;
    private Level level;
    public SineWave Sine;
    private float atY;
    private SoundSource spawnSfx;
    private Collider bounceCollider;

    public enum AppearDirection {
        Right, // default
        Left,
        Top,
        Bottom,
    }
}
