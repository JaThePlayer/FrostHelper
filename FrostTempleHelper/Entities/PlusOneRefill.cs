using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using Celeste.Mod;

namespace FrostHelper
{
    [Celeste.Mod.Entities.CustomEntity("FrostHelper/PlusOneRefill")]
    public class PlusOneRefill : Entity
    {
        bool initialized = false;
        string spritepath;
        int dashCount;
        float respawnTime;
        Color particleColor;
        bool recoverStamina;
        public PlusOneRefill(Vector2 position, bool oneUse, string spritePath, int dashCount, float respawnTime, Color particleColor, bool recStam) : base(position)
        {
            this.oneUse = oneUse;
            spritepath = spritePath;
            this.dashCount = dashCount;
            this.respawnTime = respawnTime;
            this.particleColor = particleColor;
            recoverStamina = recStam;
            Initialize(true);
        }

        public PlusOneRefill(EntityData data, Vector2 offset) : this(data.Position + offset, data.Bool("oneUse", false), data.Attr("directory", "objects/FrostHelper/plusOneRefill"), data.Int("dashCount", 1), data.Float("respawnTime", 2.5f), ColorHelper.GetColor(data.Attr("particleColor", "ffffff")), data.Bool("recoverStamina", false))
        {
        }

        public void Initialize(bool fromcctor)
        {
            initialized = true;
            if (!fromcctor)
            {
                SceneAs<Level>().ParticlesFG.Emit(Refill.P_Regen, 5, Center, Vector2.One * 4f, particleColor);
                SceneAs<Level>().ParticlesFG.Emit(Refill.P_Regen, 5, Center, Vector2.One * 4f, particleColor);
            }
            base.Collider = new Hitbox(16f, 16f, -8f, -8f);
            base.Add(new PlayerCollider(new Action<Player>(this.OnPlayer), null, null));
            string str = spritepath;
            base.Add(this.outline = new Image(GFX.Game[str + "/outline"]));
            this.outline.CenterOrigin();
            this.outline.Visible = false;
            base.Add(this.sprite = new Sprite(GFX.Game, str + "/idle"));
            this.sprite.AddLoop("idle", "", 0.1f);
            this.sprite.Play("idle", false, false);
            this.sprite.CenterOrigin();
            base.Add(this.flash = new Sprite(GFX.Game, str + "/flash"));
            this.flash.Add("flash", "", 0.05f);
            this.flash.OnFinish = delegate (string anim)
            {
                this.flash.Visible = false;
            };
            this.flash.CenterOrigin();
            base.Add(this.wiggler = Wiggler.Create(1f, 4f, delegate (float v)
            {
                this.sprite.Scale = (this.flash.Scale = Vector2.One * (1f + v * 0.2f));
            }, false, false));
            base.Add(new MirrorReflection());
            base.Add(this.bloom = new BloomPoint(0.8f, 16f));
            base.Add(this.light = new VertexLight(Color.White, 1f, 16, 40));
            base.Add(this.sine = new SineWave(0.6f));
            this.sine.Randomize();
            this.UpdateY();

            Depth = Depths.DreamBlocks - 1;
        }

        

        public override void Added(Scene scene)
        {
            base.Added(scene);
            this.level = base.SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();
            if (respawnTimer > 0f)
            {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
            else if (Scene.OnInterval(0.1f))
            {
                level.ParticlesFG.Emit(Refill.P_Glow, 1, Position, Vector2.One * 5f, particleColor);
            }
            UpdateY();
            light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
            bloom.Alpha = light.Alpha * 0.8f;
            if (Scene.OnInterval(2f) && sprite.Visible)
            {
                flash.Play("flash", true, false);
                flash.Visible = true;
            }
        }

        private void Respawn()
        {
            if (!Collidable)
            {
                Collidable = true;
                sprite.Visible = true;
                outline.Visible = false;
                Depth = Depths.DreamBlocks - 1;
                wiggler.Start();
                Audio.Play("event:/game/general/diamond_return", Position);
                level.ParticlesFG.Emit(Refill.P_Regen, 16, Position, Vector2.One * 2f, particleColor);
            }
        }

        private void UpdateY()
        {
            flash.Y = (sprite.Y = (bloom.Y = sine.Value * 2f));
        }

        public override void Render()
        {
            if (sprite != null && sprite.Visible)
            {
                sprite.DrawOutline(1);
            }
            base.Render();
        }

        private void OnPlayer(Player player)
        {
            if (player.Dashes < player.MaxDashes || (recoverStamina && player.Stamina < 20f))
            {
                player.Dashes = Math.Min(player.Dashes + dashCount, player.MaxDashes);
                if (recoverStamina)
                {
                    player.RefillStamina();
                }
                Audio.Play("event:/game/general/diamond_touch", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                Collidable = false;
                Add(new Coroutine(RefillRoutine(player), true));
                respawnTimer = respawnTime;
            }
        }

        private IEnumerator RefillRoutine(Player player)
        {
            //Celeste.Celeste.Freeze(0.05f);
            yield return null;
            level.Shake(0.3f);
            sprite.Visible = (this.flash.Visible = false);
            if (!oneUse)
            {
                outline.Visible = true;
            }
            Depth = 8999;
            yield return 0.05f;
            float angle = player.Speed.Angle();
            level.ParticlesFG.Emit(Refill.P_Shatter, 5, Position, Vector2.One * 4f, particleColor, angle - 1.57079637f);
            level.ParticlesFG.Emit(Refill.P_Shatter, 5, Position, Vector2.One * 4f, particleColor, angle + 1.57079637f);
            SlashFx.Burst(Position, angle);
            if (oneUse)
            {
                RemoveSelf();
            }
            yield break;
        }
        private Sprite sprite;

        private Sprite flash;

        private Image outline;

        private Wiggler wiggler;

        private BloomPoint bloom;

        private VertexLight light;

        private Level level;

        private SineWave sine;

        private bool oneUse;

        private float respawnTimer;
    }
}