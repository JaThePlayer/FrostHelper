using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace FrostHelper
{
    /// <summary>
    /// A booster that will kill you if you don't dash out of it
    /// </summary>
    [CustomEntity("FrostHelper/YellowBooster")]
    [Tracked]
    public class YellowBooster : Entity
    {
        public bool BoostingPlayer { get; private set; }
        public string reappearSfx;
        public string enterSfx;
        public string boostSfx;
        public string endSfx;
        public float BoostTime;
        public Color FlashTint;

        /// <summary>
        /// Set to -1 to refill dashes to dash cap (default)
        /// </summary>
        public int DashRecovery;

        public YellowBooster(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Depth = -8500;
            Collider = new Circle(10f, 0f, 2f);
            sprite = new Sprite(GFX.Game, data.Attr("directory", "objects/FrostHelper/yellowBooster/"));
            sprite.Visible = true;
            sprite.CenterOrigin();
            sprite.Justify = new Vector2(0.5f, 0.5f);
            sprite.AddLoop("loop", "booster", 0.1f, 0, 1, 2, 3, 4);
            sprite.AddLoop("inside", "booster", 0.1f, 5, 6, 7, 8);
            sprite.AddLoop("spin", "booster", 0.06f, 18, 19, 20, 21, 22, 23, 24, 25);
            sprite.Add("pop", "booster", 0.08f, 9, 10, 11, 12, 13, 14, 15, 16, 17);
            sprite.Play("loop", false);
            Add(sprite);

            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(light = new VertexLight(Color.White, 1f, 16, 32));
            Add(bloom = new BloomPoint(0.1f, 16f));
            Add(wiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
            {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }, false, false));
            Add(dashRoutine = new Coroutine(false));
            Add(dashListener = new DashListener());
            Add(new MirrorReflection());
            Add(loopingSfx = new SoundSource());
            dashListener.OnDash = new Action<Vector2>(OnPlayerDashed);
            particleType = Booster.P_Burst;

            RespawnTime = data.Float("respawnTime", 1f);
            BoostTime = data.Float("boostTime", 0.3f);
            ParticleColor = ColorHelper.GetColor(data.Attr("particleColor", "Yellow"));
            FlashTint = ColorHelper.GetColor(data.Attr("flashTint", "Red"));
            reappearSfx = data.Attr("reappearSfx", "event:/game/04_cliffside/greenbooster_reappear");
            enterSfx = data.Attr("enterSfx", "event:/game/04_cliffside/greenbooster_enter");
            boostSfx = data.Attr("boostSfx", "event:/game/04_cliffside/greenbooster_dash");
            endSfx = data.Attr("releaseSfx", "event:/game/04_cliffside/greenbooster_end");
            DashRecovery = data.Int("dashes", -1);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            Image image = new Image(GFX.Game["objects/booster/outline"]);
            image.CenterOrigin();
            image.Color = Color.White * 0.75f;
            outline = new Entity(Position);
            outline.Depth = 8999;
            outline.Visible = false;
            outline.Add(image);
            outline.Add(new MirrorReflection());
            scene.Add(outline);
        }

        public void Appear()
        {
            Audio.Play(reappearSfx, Position);
            sprite.Play("appear", false, false);
            wiggler.Start();
            Visible = true;
            AppearParticles();
        }

        private void AppearParticles()
        {
            ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
            for (int i = 0; i < 360; i += 30)
            {
                particlesBG.Emit(Booster.P_Appear, 1, Center, Vector2.One * 2f, ParticleColor, i * 0.0174532924f);
            }
        }

        private void OnPlayer(Player player)
        {
            bool flag = respawnTimer <= 0f && cannotUseTimer <= 0f && !BoostingPlayer;
            if (flag)
            {
                cannotUseTimer = 0.45f;

                Boost(player, this);

                Audio.Play(enterSfx, Position);
                wiggler.Start();
                sprite.Play("inside", false, false);
                sprite.FlipX = player.Facing == Facings.Left;
            }
        }

        public bool StartedBoosting;
        public static void Boost(Player player, YellowBooster booster)
        {
            new DynData<Player>(player).Set("fh.customyellowBooster", booster);
            player.StateMachine.State = FrostModule.YellowBoostState;
            player.Speed = Vector2.Zero;
            //player.boostTarget = booster.Center;
            //player.boostRed = false;
            FrostModule.player_boostTarget.SetValue(player, booster.Center);
            booster.StartedBoosting = true;
            //player.CurrentBooster = booster;
            //this.LastBooster = booster;
        }
        public Color ParticleColor;
        public void PlayerBoosted(Player player, Vector2 direction)
        {
            StartedBoosting = false;
            Audio.Play(boostSfx, Position);
            BoostingPlayer = true;
            Tag = Tags.Persistent | Tags.TransitionUpdate;
            sprite.Play("spin", false, false);
            sprite.FlipX = player.Facing == Facings.Left;
            outline.Visible = true;
            wiggler.Start();
            dashRoutine.Replace(BoostRoutine(player, direction));
        }

        private IEnumerator BoostRoutine(Player player, Vector2 dir)
        {
            float angle = (-dir).Angle();
            while ((player.StateMachine.State == 2 || player.StateMachine.State == 5) && BoostingPlayer)
            {
                if (player.Dead)
                {
                    PlayerDied();
                } else
                {
                    sprite.RenderPosition = player.Center + Booster.playerOffset;
                    loopingSfx.Position = sprite.Position;
                    bool flag = Scene.OnInterval(0.02f);
                    if (flag)
                    {
                        (Scene as Level).ParticlesBG.Emit(particleType, 2, player.Center - dir * 3f + new Vector2(0f, -2f), new Vector2(3f, 3f), ParticleColor, angle);
                    }
                    yield return null;
                }
                
            }
            PlayerReleased();
            bool flag2 = player.StateMachine.State == 4;
            if (flag2)
            {
                sprite.Visible = false;
            }
            while (SceneAs<Level>().Transitioning)
            {
                yield return null;
            }
            Tag = 0;
            yield break;
        }

        public void OnPlayerDashed(Vector2 direction)
        {
            bool boostingPlayer = BoostingPlayer;
            if (boostingPlayer)
            {
                BoostingPlayer = false;
            }
        }

        public void PlayerReleased()
        {
            Audio.Play(endSfx, sprite.RenderPosition);
            sprite.Play("pop", false, false);
            cannotUseTimer = 0f;
            respawnTimer = RespawnTime;
            BoostingPlayer = false;
            wiggler.Stop();
            loopingSfx.Stop(true);
        }

        public void PlayerDied()
        {
            bool boostingPlayer = BoostingPlayer;
            if (boostingPlayer)
            {
                PlayerReleased();
                dashRoutine.Active = false;
                Tag = 0;
            }
        }

        public void Respawn()
        {
            Audio.Play(reappearSfx, Position);
            sprite.Position = Vector2.Zero;
            sprite.Play("loop", true, false);
            wiggler.Start();
            sprite.Visible = true;
            outline.Visible = false;
            AppearParticles();
        }

        public override void Update()
        {
            base.Update();
            bool flag = cannotUseTimer > 0f;
            if (flag)
            {
                cannotUseTimer -= Engine.DeltaTime;
            }
            bool flag2 = respawnTimer > 0f;
            if (flag2)
            {
                respawnTimer -= Engine.DeltaTime;
                bool flag3 = respawnTimer <= 0f;
                if (flag3)
                {
                    Respawn();
                }
            }
            bool flag4 = !dashRoutine.Active && respawnTimer <= 0f;
            if (flag4)
            {
                Vector2 target = Vector2.Zero;
                Player entity = Scene.Tracker.GetEntity<Player>();
                bool flag5 = entity != null && CollideCheck(entity);
                if (flag5)
                {
                    target = entity.Center + Booster.playerOffset - Position;
                }
                sprite.Position = Calc.Approach(sprite.Position, target, 80f * Engine.DeltaTime);
            }
            bool flag6 = sprite.CurrentAnimationID == "inside" && !BoostingPlayer && !CollideCheck<Player>();
            if (flag6)
            {
                sprite.Play("loop", false, false);
            }
        }

        public override void Render()
        {
            Vector2 position = sprite.Position;
            sprite.Position = position.Floor();
            bool flag = sprite.CurrentAnimationID != "pop" && sprite.Visible;
            if (flag)
            {
                sprite.DrawOutline(1);
            }
            base.Render();
            sprite.Position = position;
        }
        
        // Note: this type is marked as 'beforefieldinit'.
        static YellowBooster()
        {
            playerOffset = new Vector2(0f, -2f);
        }

        private float RespawnTime;

        public static ParticleType P_Burst { get {return Booster.P_Burst; } }

        public static ParticleType P_BurstRed { get {return Booster.P_BurstRed; } }

        public static ParticleType P_Appear { get {return Booster.P_Appear; } }

        public static ParticleType P_RedAppear { get {return Booster.P_RedAppear; } }

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
