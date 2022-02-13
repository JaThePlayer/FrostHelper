using Celeste.Mod.Entities;

namespace FrostHelper {
    [CustomEntity("FrostHelper/StaticBumper")]
    public class StaticBumper : Entity {
        public bool Wobbling = false;
        public bool NotCoreMode = false;
        public StaticBumper(EntityData data, Vector2 offset) : base(data.Position + offset) {
            respawnTime = data.Float("respawnTime", 0.6f);
            // sprite is a pointer to a spritebank's xml
            string path = data.Attr("sprite", "bumper");
            Wobbling = data.Bool("wobble", false);
            NotCoreMode = data.Bool("notCoreMode", false);
            moveTime = data.Float("moveTime", MoveCycleTime);
            Collider = new Circle(12f, 0f, 0f);
            Add(new PlayerCollider(OnPlayer, null, null));
            Add(sine = new SineWave(0.44f, 0f).Randomize());
            Add(sprite = GFX.SpriteBank.Create(path));
            Add(spriteEvil = GFX.SpriteBank.Create($"{path}_evil"));
            spriteEvil.Visible = false;
            Add(light = new VertexLight(Color.Teal, 1f, 16, 32));
            Add(bloom = new BloomPoint(0.5f, 16f));
            anchor = Position;
            Vector2? node = data.FirstNodeNullable(new Vector2?(offset));

            if (node is not null) {
                Vector2 start = Position;
                Vector2 end = node.Value;
                Ease.Easer ease = EaseHelper.GetEase(data.Attr("easing", "CubeInOut"));
                Tween tween = Tween.Create(Tween.TweenMode.Looping, ease, moveTime, true);
                tween.OnUpdate = t => {
                    if (goBack) {
                        anchor = Vector2.Lerp(end, start, t.Eased);
                    } else {
                        anchor = Vector2.Lerp(start, end, t.Eased);
                    }
                };
                tween.OnComplete = t => {
                    goBack = !goBack;
                };
                Add(tween);
            }
            UpdatePosition();
            Add(hitWiggler = Wiggler.Create(1.2f, 2f, v => spriteEvil.Position = hitDir * hitWiggler!.Value * 8f, false, false));
            if (!NotCoreMode)
                Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));

        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (NotCoreMode) {
                fireMode = false;
            } else {
                fireMode = SceneAs<Level>().CoreMode == Session.CoreModes.Hot;
            }
            spriteEvil.Visible = fireMode;
            sprite.Visible = !fireMode;
        }

        private void OnChangeMode(Session.CoreModes coreMode) {
            fireMode = coreMode == Session.CoreModes.Hot;
            spriteEvil.Visible = fireMode;
            sprite.Visible = !fireMode;
        }

        private void UpdatePosition() {
            Position = anchor;
            if (Wobbling) {
                Position += new Vector2(sine.Value * 3f, sine.ValueOverTwo * 2f);
            }
        }

        public override void Update() {
            base.Update();
            if (respawnTimer > 0f) {
                respawnTimer -= Engine.DeltaTime;

                if (respawnTimer <= 0f) {
                    light.Visible = true;
                    bloom.Visible = true;
                    sprite.Play("on", false, false);
                    spriteEvil.Play("on", false, false);
                    bool flag3 = !fireMode;
                    if (flag3) {
                        Audio.Play("event:/game/06_reflection/pinballbumper_reset", Position);
                    }
                }
            } else if (Scene.OnInterval(0.05f)) {
                float angle = Calc.Random.NextAngle();
                ParticleType type = fireMode ? Bumper.P_FireAmbience : Bumper.P_Ambience;
                float direction = fireMode ? -1.57079637f : angle;
                float length = fireMode ? 12 : 8;
                SceneAs<Level>().Particles.Emit(type, 1, Center + Calc.AngleToVector(angle, length), Vector2.One * 2f, direction);
            }
            UpdatePosition();
        }

        private void OnPlayer(Player player) {
            if (fireMode && !SaveData.Instance.Assists.Invincible) {
                Vector2 vector = (player.Center - Center).SafeNormalize();
                hitDir = -vector;
                hitWiggler.Start();
                Audio.Play("event:/game/09_core/hotpinball_activate", Position);
                respawnTimer = respawnTime;
                player.Die(vector, false, true);
                SceneAs<Level>().Particles.Emit(Bumper.P_FireHit, 12, Center + vector * 12f, Vector2.One * 3f, vector.Angle());
            } else if (respawnTimer <= 0f) {
                if ((Scene as Level)!.Session.Area.ID == 9) {
                    Audio.Play("event:/game/09_core/pinballbumper_hit", Position);
                } else {
                    Audio.Play("event:/game/06_reflection/pinballbumper_hit", Position);
                }
                respawnTimer = respawnTime;
                Vector2 vector2 = player.ExplodeLaunch(Position, false, false);
                sprite.Play("hit", true, false);
                spriteEvil.Play("hit", true, false);
                light.Visible = false;
                bloom.Visible = false;
                SceneAs<Level>().DirectionalShake(vector2, 0.15f);
                SceneAs<Level>().Displacement.AddBurst(Center, 0.3f, 8f, 32f, 0.8f, null, null);
                SceneAs<Level>().Particles.Emit(Bumper.P_Launch, 12, Center + vector2 * 12f, Vector2.One * 3f, vector2.Angle());
            }
        }

        private float respawnTime = 0.6f;

        private float moveTime;
        private const float MoveCycleTime = 1.81818187f;

        private const float SineCycleFreq = 0.44f;

        private Sprite sprite;

        private Sprite spriteEvil;

        private VertexLight light;

        private BloomPoint bloom;

        private bool goBack;

        private Vector2 anchor;

        private SineWave sine;

        private float respawnTimer;

        private bool fireMode;

        private Wiggler hitWiggler;

        private Vector2 hitDir;
    }
}
