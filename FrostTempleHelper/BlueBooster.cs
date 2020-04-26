using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostTempleHelper
{
    /// <summary>
    /// A booster that doesn't recover your dash
    /// </summary>
    [CustomEntity("FrostHelper/BlueBooster")]
    [Tracked]
    public class BlueBooster : Entity
    {
        public bool BoostingPlayer { get; private set; }
        public string reappearSfx;
        public string enterSfx;
        public string boostSfx;
        public string endSfx;
        public float BoostTime;
        public BlueBooster(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            base.Depth = -8500;
            base.Collider = new Circle(10f, 0f, 2f);
            this.sprite = new Sprite(GFX.Game, data.Attr("directory", "objects/FrostHelper/blueBooster/"));
            sprite.Visible = true;
            sprite.CenterOrigin();
            sprite.Justify = (new Vector2(0.5f, 0.5f));
            sprite.AddLoop("loop", "booster", 0.1f, 0, 1, 2, 3, 4);
            sprite.AddLoop("inside", "booster", 0.1f, 5, 6, 7, 8);
            sprite.AddLoop("spin", "booster", 0.06f, 18, 19, 20, 21, 22, 23, 24, 25);
            sprite.Add("pop", "booster", 0.08f, 9, 10, 11, 12, 13, 14, 15, 16, 17);
            sprite.Play("loop", false);
            Add(sprite);

            base.Add(new PlayerCollider(new Action<Player>(this.OnPlayer), null, null));
            base.Add(this.light = new VertexLight(Color.White, 1f, 16, 32));
            base.Add(this.bloom = new BloomPoint(0.1f, 16f));
            base.Add(this.wiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
            {
                this.sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }, false, false));
            base.Add(this.dashRoutine = new Coroutine(false));
            base.Add(this.dashListener = new DashListener());
            base.Add(new MirrorReflection());
            base.Add(this.loopingSfx = new SoundSource());
            this.dashListener.OnDash = new Action<Vector2>(this.OnPlayerDashed);
            this.particleType = (Booster.P_Burst);

            RespawnTime = data.Float("respawnTime", 1f);
            BoostTime = data.Float("boostTime", 0.3f);
            ParticleColor = FrostHelper.ColorHelper.GetColor(data.Attr("particleColor", "Yellow"));
            reappearSfx = data.Attr("reappearSfx", "event:/game/04_cliffside/greenbooster_reappear");
            enterSfx = data.Attr("enterSfx", "event:/game/04_cliffside/greenbooster_enter");
            boostSfx = data.Attr("boostSfx", "event:/game/04_cliffside/greenbooster_dash");
            endSfx = data.Attr("releaseSfx", "event:/game/04_cliffside/greenbooster_end");
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            Image image = new Image(GFX.Game["objects/booster/outline"]);
            image.CenterOrigin();
            image.Color = Color.White * 0.75f;
            this.outline = new Entity(this.Position);
            this.outline.Depth = 8999;
            this.outline.Visible = false;
            this.outline.Add(image);
            this.outline.Add(new MirrorReflection());
            scene.Add(this.outline);
        }

        public void Appear()
        {
            Audio.Play(reappearSfx, this.Position);
            this.sprite.Play("appear", false, false);
            this.wiggler.Start();
            this.Visible = true;
            this.AppearParticles();
        }

        private void AppearParticles()
        {
            ParticleSystem particlesBG = base.SceneAs<Level>().ParticlesBG;
            for (int i = 0; i < 360; i += 30)
            {
                particlesBG.Emit(Booster.P_Appear, 1, base.Center, Vector2.One * 2f, ParticleColor, i * 0.0174532924f);
            }
        }

        private void OnPlayer(Player player)
        {
            bool flag = this.respawnTimer <= 0f && this.cannotUseTimer <= 0f && !this.BoostingPlayer;
            if (flag)
            {
                this.cannotUseTimer = 0.45f;

                Boost(player, this);

                Audio.Play(enterSfx, this.Position);
                this.wiggler.Start();
                this.sprite.Play("inside", false, false);
                this.sprite.FlipX = (player.Facing == Facings.Left);
            }
        }

        public bool StartedBoosting;
        public static void Boost(Player player, BlueBooster booster)
        {
            player.StateMachine.State = FrostHelper.FrostModule.blueBoostState;
            player.Speed = Vector2.Zero;
            //player.boostTarget = booster.Center;
            //player.boostRed = false;
            FrostHelper.FrostModule.player_boostTarget.SetValue(player, booster.Center);
            booster.StartedBoosting = true;
            //player.CurrentBooster = booster;
            //this.LastBooster = booster;
        }
        public Color ParticleColor;
        public void PlayerBoosted(Player player, Vector2 direction)
        {
            StartedBoosting = false;
            Audio.Play(boostSfx, this.Position);
            this.BoostingPlayer = true;
            base.Tag = (Tags.Persistent | Tags.TransitionUpdate);
            this.sprite.Play("spin", false, false);
            this.sprite.FlipX = (player.Facing == Facings.Left);
            this.outline.Visible = true;
            this.wiggler.Start();
            this.dashRoutine.Replace(this.BoostRoutine(player, direction));
        }

        private IEnumerator BoostRoutine(Player player, Vector2 dir)
        {
            float angle = (-dir).Angle();
            while ((player.StateMachine.State == 2 || player.StateMachine.State == 5) && this.BoostingPlayer)
            {
                if (player.Dead)
                {
                    PlayerDied();
                } else
                {
                    this.sprite.RenderPosition = player.Center + Booster.playerOffset;
                    this.loopingSfx.Position = this.sprite.Position;
                    bool flag = this.Scene.OnInterval(0.02f);
                    if (flag)
                    {
                        (this.Scene as Level).ParticlesBG.Emit(this.particleType, 2, player.Center - dir * 3f + new Vector2(0f, -2f), new Vector2(3f, 3f), ParticleColor, angle);
                    }
                    yield return null;
                }
                
            }
            this.PlayerReleased();
            bool flag2 = player.StateMachine.State == 4;
            if (flag2)
            {
                this.sprite.Visible = false;
            }
            while (this.SceneAs<Level>().Transitioning)
            {
                yield return null;
            }
            this.Tag = 0;
            yield break;
        }

        public void OnPlayerDashed(Vector2 direction)
        {
            bool boostingPlayer = this.BoostingPlayer;
            if (boostingPlayer)
            {
                this.BoostingPlayer = false;
            }
        }

        public void PlayerReleased()
        {
            Audio.Play(endSfx, this.sprite.RenderPosition);
            this.sprite.Play("pop", false, false);
            this.cannotUseTimer = 0f;
            this.respawnTimer = RespawnTime;
            this.BoostingPlayer = false;
            this.wiggler.Stop();
            this.loopingSfx.Stop(true);
        }

        public void PlayerDied()
        {
            bool boostingPlayer = this.BoostingPlayer;
            if (boostingPlayer)
            {
                this.PlayerReleased();
                this.dashRoutine.Active = false;
                base.Tag = 0;
            }
        }

        public void Respawn()
        {
            Audio.Play(reappearSfx, this.Position);
            this.sprite.Position = Vector2.Zero;
            this.sprite.Play("loop", true, false);
            this.wiggler.Start();
            this.sprite.Visible = true;
            this.outline.Visible = false;
            this.AppearParticles();
        }

        public override void Update()
        {
            base.Update();
            bool flag = this.cannotUseTimer > 0f;
            if (flag)
            {
                this.cannotUseTimer -= Engine.DeltaTime;
            }
            bool flag2 = this.respawnTimer > 0f;
            if (flag2)
            {
                this.respawnTimer -= Engine.DeltaTime;
                bool flag3 = this.respawnTimer <= 0f;
                if (flag3)
                {
                    this.Respawn();
                }
            }
            bool flag4 = !this.dashRoutine.Active && this.respawnTimer <= 0f;
            if (flag4)
            {
                Vector2 target = Vector2.Zero;
                Player entity = base.Scene.Tracker.GetEntity<Player>();
                bool flag5 = entity != null && base.CollideCheck(entity);
                if (flag5)
                {
                    target = entity.Center + Booster.playerOffset - this.Position;
                }
                this.sprite.Position = Calc.Approach(this.sprite.Position, target, 80f * Engine.DeltaTime);
            }
            bool flag6 = this.sprite.CurrentAnimationID == "inside" && !this.BoostingPlayer && !base.CollideCheck<Player>();
            if (flag6)
            {
                this.sprite.Play("loop", false, false);
            }
        }

        public override void Render()
        {
            Vector2 position = this.sprite.Position;
            this.sprite.Position = position.Floor();
            bool flag = this.sprite.CurrentAnimationID != "pop" && this.sprite.Visible;
            if (flag)
            {
                this.sprite.DrawOutline(1);
            }
            base.Render();
            this.sprite.Position = position;
        }
        
        // Note: this type is marked as 'beforefieldinit'.
        static BlueBooster()
        {
            playerOffset = new Vector2(0f, -2f);
        }

        private float RespawnTime;

        public static ParticleType P_Burst => Booster.P_Burst;

        public static ParticleType P_BurstRed => Booster.P_BurstRed;

        public static ParticleType P_Appear => Booster.P_Appear;

        public static ParticleType P_RedAppear => Booster.P_RedAppear;

        public static readonly Vector2 playerOffset;

        public Sprite sprite;

        private Entity outline;

        private Wiggler wiggler;

        private BloomPoint bloom;

        private VertexLight light;

        private Coroutine dashRoutine;

        private DashListener dashListener;

        private ParticleType particleType;

        private float respawnTimer;

        private float cannotUseTimer;

        //private bool red = false;

        private SoundSource loopingSfx;
    }
}
