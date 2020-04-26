using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;

namespace FrostHelper
{
    public class StaticBumper : Entity
    {
        public bool Wobbling = false;
        public bool NotCoreMode = false;
        public StaticBumper(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            respawnTime = data.Float("respawnTime", 0.6f);
            // sprite is a pointer to a spritebank's xml
            string path = data.Attr("sprite", "bumper");
            Wobbling = data.Bool("wobble", false);
            NotCoreMode = data.Bool("notCoreMode", false);
            moveTime = data.Float("moveTime", MoveCycleTime);
            Collider = new Circle(12f, 0f, 0f);
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(sine = new SineWave(0.44f, 0f).Randomize());
            Add(sprite = GFX.SpriteBank.Create(path));
            Add(spriteEvil = GFX.SpriteBank.Create($"{path}_evil"));
            spriteEvil.Visible = false;
            Add(light = new VertexLight(Color.Teal, 1f, 16, 32));
            Add(bloom = new BloomPoint(0.5f, 16f));
            anchor = Position;
            Vector2? node = data.FirstNodeNullable(new Vector2?(offset));
            bool flag = node != null;
            if (flag)
            {
                Vector2 start = this.Position;
                Vector2 end = node.Value;
                Ease.Easer ease = EaseHelper.GetEase(data.Attr("easing", "CubeInOut"));
                Tween tween = Tween.Create(Tween.TweenMode.Looping, ease, moveTime, true);
                tween.OnUpdate = delegate (Tween t)
                {
                    bool flag2 = this.goBack;
                    if (flag2)
                    {
                        this.anchor = Vector2.Lerp(end, start, t.Eased);
                    }
                    else
                    {
                        this.anchor = Vector2.Lerp(start, end, t.Eased);
                    }
                };
                tween.OnComplete = delegate (Tween t)
                {
                    this.goBack = !this.goBack;
                };
                base.Add(tween);
            }
            UpdatePosition();
            Add(hitWiggler = Wiggler.Create(1.2f, 2f, delegate (float v)
            {
                spriteEvil.Position = hitDir * hitWiggler.Value * 8f;
            }, false, false));
            if (!NotCoreMode)
            Add(new CoreModeListener(new Action<Session.CoreModes>(OnChangeMode)));
            
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            if (NotCoreMode)
            {
                fireMode = false;
            } else
            {
                fireMode = (base.SceneAs<Level>().CoreMode == Session.CoreModes.Hot);
            }
            spriteEvil.Visible = fireMode;
            sprite.Visible = !fireMode;
        }

        private void OnChangeMode(Session.CoreModes coreMode)
        {
            fireMode = (coreMode == Session.CoreModes.Hot);
            spriteEvil.Visible = fireMode;
            sprite.Visible = !fireMode;
        }

        private void UpdatePosition()
        {
            Position = anchor;
            if (Wobbling)
            {
                Position += new Vector2(sine.Value * 3f, sine.ValueOverTwo * 2f);
            }
        }

        public override void Update()
        {
            base.Update();
            bool flag = this.respawnTimer > 0f;
            if (flag)
            {
                this.respawnTimer -= Engine.DeltaTime;
                bool flag2 = this.respawnTimer <= 0f;
                if (flag2)
                {
                    this.light.Visible = true;
                    this.bloom.Visible = true;
                    this.sprite.Play("on", false, false);
                    this.spriteEvil.Play("on", false, false);
                    bool flag3 = !this.fireMode;
                    if (flag3)
                    {
                        Audio.Play("event:/game/06_reflection/pinballbumper_reset", this.Position);
                    }
                }
            }
            else
            {
                bool flag4 = base.Scene.OnInterval(0.05f);
                if (flag4)
                {
                    float num = Calc.Random.NextAngle();
                    ParticleType type = this.fireMode ? Bumper.P_FireAmbience : Bumper.P_Ambience;
                    float direction = this.fireMode ? -1.57079637f : num;
                    float length = (float)(this.fireMode ? 12 : 8);
                    base.SceneAs<Level>().Particles.Emit(type, 1, base.Center + Calc.AngleToVector(num, length), Vector2.One * 2f, direction);
                }
            }
            UpdatePosition();
        }

        private void OnPlayer(Player player)
        {
            bool flag = this.fireMode;
            if (flag)
            {
                bool flag2 = !SaveData.Instance.Assists.Invincible;
                if (flag2)
                {
                    Vector2 vector = (player.Center - base.Center).SafeNormalize();
                    this.hitDir = -vector;
                    this.hitWiggler.Start();
                    Audio.Play("event:/game/09_core/hotpinball_activate", this.Position);
                    this.respawnTimer = respawnTime;
                    player.Die(vector, false, true);
                    base.SceneAs<Level>().Particles.Emit(Bumper.P_FireHit, 12, base.Center + vector * 12f, Vector2.One * 3f, vector.Angle());
                }
            }
            else
            {
                bool flag3 = this.respawnTimer <= 0f;
                if (flag3)
                {
                    bool flag4 = (base.Scene as Level).Session.Area.ID == 9;
                    if (flag4)
                    {
                        Audio.Play("event:/game/09_core/pinballbumper_hit", this.Position);
                    }
                    else
                    {
                        Audio.Play("event:/game/06_reflection/pinballbumper_hit", this.Position);
                    }
                    this.respawnTimer = respawnTime;
                    Vector2 vector2 = player.ExplodeLaunch(this.Position, false, false);
                    this.sprite.Play("hit", true, false);
                    this.spriteEvil.Play("hit", true, false);
                    this.light.Visible = false;
                    this.bloom.Visible = false;
                    base.SceneAs<Level>().DirectionalShake(vector2, 0.15f);
                    base.SceneAs<Level>().Displacement.AddBurst(base.Center, 0.3f, 8f, 32f, 0.8f, null, null);
                    base.SceneAs<Level>().Particles.Emit(Bumper.P_Launch, 12, base.Center + vector2 * 12f, Vector2.One * 3f, vector2.Angle());
                }
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
