using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/PlusOneRefill")]
internal sealed class PlusOneRefill : Entity {
    private readonly int _dashCount;
    private readonly float _respawnTime;
    private readonly Color _particleColor;
    private readonly bool _recoverStamina;

    public PlusOneRefill(EntityData data, Vector2 offset) : base(data.Position + offset) {
        _oneUse = data.Bool("oneUse", false);
        _dashCount = data.Int("dashCount", 1);
        _respawnTime = data.Float("respawnTime", 2.5f);
        Collider = data.Collider("hitbox") ?? new Hitbox(16f, 16f, -8f, -8f);
        _particleColor = data.GetColor("particleColor", "ffffff");
        _recoverStamina = data.Bool("recoverStamina", false);
        var directory = data.Attr("directory", "objects/FrostHelper/plusOneRefill");
        
        Add(new PlayerCollider(OnPlayer));
        Add(_outline = new Image(GFX.Game[directory + "/outline"]));
        _outline.CenterOrigin();
        _outline.Visible = false;
        Add(_sprite = new Sprite(GFX.Game, directory + "/idle"));
        _sprite.AddLoop("idle", "", 0.1f);
        _sprite.Play("idle", false, false);
        _sprite.CenterOrigin();
        Add(_flash = new Sprite(GFX.Game, directory + "/flash"));
        _flash.Add("flash", "", 0.05f);
        _flash.OnFinish = _ => {
            _flash.Visible = false;
        };
        _flash.CenterOrigin();
        Add(_wiggler = Wiggler.Create(1f, 4f, v => {
            _sprite.Scale = _flash.Scale = Vector2.One * (1f + v * 0.2f);
        }));
        Add(new MirrorReflection());
        Add(_bloom = data.GetBloomPoint("bloom", 0.8f, 16f));
        Add(_light = data.GetVertexLight("light", Color.White, 1f, 16, 40));
        Add(_sine = new SineWave(0.6f));
        _sine.Randomize();
        UpdateY();

        Depth = Depths.DreamBlocks - 1;
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        _level = SceneAs<Level>();
    }

    public override void Update() {
        base.Update();
        if (_respawnTimer > 0f) {
            _respawnTimer -= Engine.DeltaTime;
            if (_respawnTimer <= 0f) {
                Respawn();
            }
        } else if (Scene.OnInterval(0.1f)) {
            _level.ParticlesFG.Emit(Refill.P_Glow, 1, Position, Vector2.One * 5f, _particleColor);
        }
        UpdateY();
        _light.Alpha = Calc.Approach(_light.Alpha, _sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
        _bloom.Alpha = _light.Alpha * 0.8f;
        if (Scene.OnInterval(2f) && _sprite.Visible) {
            _flash.Play("flash", true, false);
            _flash.Visible = true;
        }
    }

    private void Respawn() {
        if (!Collidable) {
            Collidable = true;
            _sprite.Visible = true;
            _outline.Visible = false;
            Depth = Depths.DreamBlocks - 1;
            _wiggler.Start();
            Audio.Play("event:/game/general/diamond_return", Position);
            _level.ParticlesFG.Emit(Refill.P_Regen, 16, Position, Vector2.One * 2f, _particleColor);
        }
    }

    private void UpdateY() {
        _flash.Y = _sprite.Y = _bloom.Y = _sine.Value * 2f;
    }

    public override void Render() {
        if (_sprite != null && _sprite.Visible) {
            _sprite.DrawOutline(1);
        }
        base.Render();
    }

    private void OnPlayer(Player player) {
        if (player.Dashes < player.MaxDashes || (_recoverStamina && player.Stamina < 20f)) {
            player.Dashes = Math.Min(player.Dashes + _dashCount, player.MaxDashes);
            if (_recoverStamina) {
                player.RefillStamina();
            }
            Audio.Play("event:/game/general/diamond_touch", Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;
            Add(new Coroutine(RefillRoutine(player), true));
            _respawnTimer = _respawnTime;
        }
    }

    private IEnumerator RefillRoutine(Player player) {
        Celeste.Celeste.Freeze(0.05f);
        yield return null;
        _level.Shake(0.3f);
        _sprite.Visible = _flash.Visible = false;
        if (!_oneUse) {
            _outline.Visible = true;
        }
        Depth = 8999;
        yield return 0.05f;
        float angle = player.Speed.Angle();
        _level.ParticlesFG.Emit(Refill.P_Shatter, 5, Position, Vector2.One * 4f, _particleColor, angle - 1.57079637f);
        _level.ParticlesFG.Emit(Refill.P_Shatter, 5, Position, Vector2.One * 4f, _particleColor, angle + 1.57079637f);
        SlashFx.Burst(Position, angle);
        if (_oneUse) {
            RemoveSelf();
        }
    }
    private readonly Sprite _sprite;

    private readonly Sprite _flash;

    private readonly Image _outline;

    private readonly Wiggler _wiggler;

    private readonly BloomPoint _bloom;

    private readonly VertexLight _light;

    private Level _level;

    private readonly SineWave _sine;

    private readonly bool _oneUse;

    private float _respawnTimer;
}