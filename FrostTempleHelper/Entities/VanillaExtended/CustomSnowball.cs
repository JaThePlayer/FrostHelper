using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace FrostHelper
{
    public class CustomSnowball : Entity
    {
        public float Speed;
        public float ResetTime;

        public bool DrawOutline;

        public CustomSnowball(string spritePath = "snowball", float speed = 200f, float resetTime = 0.8f, float sineWaveFrequency = 0.5f, bool drawOutline = true)
        {
            Speed = speed;
            ResetTime = resetTime;
            DrawOutline = drawOutline;
            Depth = -12500;

            Collider = new Hitbox(12f, 9f, -5f, -2f);
            bounceCollider = new Hitbox(16f, 6f, -6f, -8f);

            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(new PlayerCollider(new Action<Player>(OnPlayerBounce), bounceCollider, null));
            Add(Sine = new SineWave(sineWaveFrequency, 0f));

            CreateSprite(spritePath);

            Sprite.Play("spin", false, false);
            Add(spawnSfx = new SoundSource());
        }

        public void CreateSprite(string path)
        {
            Sprite?.RemoveSelf();
            Add(Sprite = GFX.SpriteBank.Create(path));
        }
        
        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
            ResetPosition();
        }
        
        private void ResetPosition()
        {
            Player entity = level.Tracker.GetEntity<Player>();
            if (entity != null && entity.Right < (float)(level.Bounds.Right - 64))
            {
                spawnSfx.Play("event:/game/04_cliffside/snowball_spawn", null, 0f);
                Collidable = (Visible = true);
                resetTimer = 0f;
                X = level.Camera.Right + 10f;
                atY = (Y = entity.CenterY);
                Sine.Reset();
                Sprite.Play("spin", false, false);
                return;
            }
            resetTimer = 0.05f;
        }
        
        private void Destroy()
        {
            Collidable = false;
            Sprite.Play("break", false, false);
        }
        
        private void OnPlayer(Player player)
        {
            player.Die(new Vector2(-1f, 0f), false, true);
            Destroy();
            Audio.Play("event:/game/04_cliffside/snowball_impact", Position);
        }
        
        private void OnPlayerBounce(Player player)
        {
            if (!CollideCheck(player))
            {
                Celeste.Celeste.Freeze(0.1f);
                player.Bounce(Top - 2f);
                Destroy();
                Audio.Play("event:/game/general/thing_booped", Position);
            }
        }
        
        public override void Update()
        {
            base.Update();
            X -= Speed * Engine.DeltaTime;
            Y = atY + 4f * Sine.Value;
            if (X < level.Camera.Left - 60f)
            {
                resetTimer += Engine.DeltaTime;
                if (resetTimer >= ResetTime)
                {
                    ResetPosition();
                }
            }
        }
        
        public override void Render()
        {
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
    }
}
