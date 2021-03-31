using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace FrostHelper
{
    [Tracked(false)]
    [CustomEntity("FrostHelper/CustomFeather")]
    public class CustomFeather : Entity
    {
        /// <summary>
        /// the maximum speed you fly with this feather
        /// </summary>
        public float MaxSpeed;
        /// <summary>
        /// the lowest speed you fly with this feather while holding a direction
        /// </summary>
        public float LowSpeed;

        /// <summary>
        /// The speed you fly with when not holding any direction
        /// </summary>
        public float NeutralSpeed;

        public CustomFeather(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            shielded = data.Bool("shielded", false);
            singleUse = data.Bool("singleUse", false);
            RespawnTime = data.Float("respawnTime", 3f);
            FlyColor = ColorHelper.GetColor(data.Attr("flyColor", "ffd65c"));
            FlyTime = data.Float("flyTime", 2f);
            MaxSpeed = data.Float("maxSpeed", 190f);
            LowSpeed = data.Float("lowSpeed", 140f);
            NeutralSpeed = data.Float("neutralSpeed", 91f);
            Collider = new Hitbox(20f, 20f, -10f, -10f);
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            string path = data.Attr("spritePath", "objects/flyFeather/").Replace('\\', '/');
            if (path[path.Length-1] != '/')
            {
                path += '/';
            }
            sprite = new Sprite(GFX.Game, path)
            {
                Visible = true
            };
            sprite.CenterOrigin();
            sprite.Color = ColorHelper.GetColor(data.Attr("spriteColor", "White"));
            sprite.Justify = new Vector2(0.5f, 0.5f);
            sprite.Add("loop", "idle", 0.06f, "flash");
            sprite.Add("flash", "flash", 0.06f, "loop");
            sprite.Play("loop");
            Add(sprite);
            
            Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v)
            {
                sprite.Scale = Vector2.One * (1f + v * 0.2f);
            }, false, false));
            Add(bloom = new BloomPoint(0.5f, 20f));
            Add(light = new VertexLight(Color.White, 1f, 16, 48));
            Add(sine = new SineWave(0.6f, 0f).Randomize());
            Add(outline = new Image(GFX.Game[data.Attr("outlinePath", "objects/flyFeather/outline")]));
            outline.CenterOrigin();
            outline.Visible = false;
            shieldRadiusWiggle = Wiggler.Create(0.5f, 4f, null, false, false);
            Add(shieldRadiusWiggle);
            moveWiggle = Wiggler.Create(0.8f, 2f, null, false, false);
            moveWiggle.StartZero = true;
            Add(moveWiggle);
            UpdateY();

            P_Collect = new ParticleType(FlyFeather.P_Collect)
            {
                ColorMode = ParticleType.ColorModes.Static,
                Color = FlyColor
            };
            P_Flying = new ParticleType(FlyFeather.P_Flying)
            {
                ColorMode = ParticleType.ColorModes.Static,
                Color = FlyColor
            };
            P_Boost = new ParticleType(FlyFeather.P_Boost)
            {
                ColorMode = ParticleType.ColorModes.Static,
                Color = FlyColor
            };
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();
            bool flag = respawnTimer > 0f;
            if (flag)
            {
                respawnTimer -= Engine.DeltaTime;
                bool flag2 = respawnTimer <= 0f;
                if (flag2)
                {
                    Respawn();
                }
            }
            UpdateY();
            light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
            bloom.Alpha = light.Alpha * 0.8f;
        }

        public override void Render()
        {
            base.Render();
            bool flag = shielded && sprite.Visible;
            if (flag)
            {
                Draw.Circle(Position + sprite.Position, 10f - shieldRadiusWiggle.Value * 2f, Color.White, 3);
            }
        }

        private void Respawn()
        {
            bool flag = !Collidable;
            if (flag)
            {
                outline.Visible = false;
                Collidable = true;
                sprite.Visible = true;
                wiggler.Start();
                Audio.Play("event:/game/06_reflection/feather_reappear", Position);
                level.ParticlesFG.Emit(FlyFeather.P_Respawn, 16, Position, Vector2.One * 2f, FlyColor);
            }
        }

        private void UpdateY()
        {
            sprite.X = 0f;
            sprite.Y = (bloom.Y = sine.Value * 2f);
            sprite.Position += moveWiggleDir * moveWiggle.Value * -8f;
        }

        private void OnPlayer(Player player)
        {
            Vector2 speed = player.Speed;
            bool flag = shielded && !player.DashAttacking;
            if (flag)
            {
                player.PointBounce(Center);
                moveWiggle.Start();
                shieldRadiusWiggle.Start();
                moveWiggleDir = (Center - player.Center).SafeNormalize(Vector2.UnitY);
                Audio.Play("event:/game/06_reflection/feather_bubble_bounce", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            }
            else
            {
                if (StartStarFly(player))
                {
                    if (player.StateMachine.State != FrostModule.CustomFeatherState && player.StateMachine.State != Player.StStarFly)
                    {
                        Audio.Play(shielded ? "event:/game/06_reflection/feather_bubble_get" : "event:/game/06_reflection/feather_get", Position);
                    }
                    else
                    {
                        Audio.Play(shielded ? "event:/game/06_reflection/feather_bubble_renew" : "event:/game/06_reflection/feather_renew", Position);
                    }
                    Collidable = false;
                    Add(new Coroutine(CollectRoutine(player, speed), true));
                    bool flag5 = !singleUse;
                    if (flag5)
                    {
                        outline.Visible = true;
                        respawnTimer = RespawnTime;
                    }
                }
            }
        }

        public Color FlyColor;
        public float FlyTime;

        public bool StartStarFly(Player player)
        {
            DynData<Player> data = new DynData<Player>(player);
            player.RefillStamina();
            bool result;
            if (player.StateMachine.State == Player.StReflectionFall)
            {
                result = false;
            }
            else
            {
                data["fh.customFeather"] = this;
                if (player.StateMachine.State == FrostModule.CustomFeatherState)
                {
                    data["starFlyTimer"] = FlyTime;
                    player.Sprite.Color = FlyColor;
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                }
                else
                {
                    player.StateMachine.State = FrostModule.CustomFeatherState;
                }
                result = true;
            }
            return result;
        }
        
        private IEnumerator CollectRoutine(Player player, Vector2 playerSpeed)
        {
            level.Shake(0.3f);
            sprite.Visible = false;
            yield return 0.05f;
            float angle = 0f;
            bool flag = playerSpeed != Vector2.Zero;
            if (flag)
            {
                angle = playerSpeed.Angle();
            }
            else
            {
                angle = (Position - player.Center).Angle();
            }
            level.ParticlesFG.Emit(P_Collect, 10, Position, Vector2.One * 6f, FlyColor);
            SlashFx.Burst(Position, angle);
            yield break;
        }

        public ParticleType P_Collect;

        public ParticleType P_Boost;

        public ParticleType P_Flying;

        //public static ParticleType P_Respawn;

        private float RespawnTime = 3f;

        private Sprite sprite;

        private Image outline;

        private Wiggler wiggler;

        private BloomPoint bloom;

        private VertexLight light;

        private Level level;

        private SineWave sine;

        private bool shielded;

        private bool singleUse;

        private Wiggler shieldRadiusWiggle;

        private Wiggler moveWiggle;

        private Vector2 moveWiggleDir;

        private float respawnTimer;
    }
}
