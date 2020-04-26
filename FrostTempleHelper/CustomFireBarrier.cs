using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using Celeste.Mod.Entities;

namespace FrostTempleHelper
{
    [CustomEntity("FrostHelper/CustomFireBarrier")]
    public class CustomFireBarrier : Entity
    {
        private bool isIce;

        public CustomFireBarrier(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Color[] colors = new Color[3];
            colors[0] = data.HexColor("surfaceColor");
            colors[1] = data.HexColor("edgeColor");
            colors[2] = data.HexColor("centerColor");
            float width = data.Width;
            float height = data.Height;
            this.isIce = data.Bool("isIce", false);
            Tag = Tags.TransitionUpdate;
            Collider = new Hitbox(width, height, 0f, 0f);
            Add(new PlayerCollider(new Action<Player>(this.OnPlayer), null, null));
            Add(new CoreModeListener(new Action<Session.CoreModes>(this.OnChangeMode)));
            Add(Lava = new LavaRect(width, height, 4));
            Lava.SurfaceColor = colors[0];
            Lava.EdgeColor = colors[1];
            Lava.CenterColor = colors[2];
            Lava.SmallWaveAmplitude = 2f;
            Lava.BigWaveAmplitude = 1f;
            Lava.CurveAmplitude = 1f;
            Depth = -8500;
            Add(this.idleSfx = new SoundSource());
            idleSfx.Position = new Vector2(base.Width, base.Height) / 2f;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            scene.Add(this.solid = new Solid(this.Position + new Vector2(2f, 3f), base.Width - 4f, base.Height - 5f, false));
            if (!isIce)
            {
                Collidable = (solid.Collidable = (SceneAs<Level>().CoreMode == Session.CoreModes.Hot));
            } else
            {
                Collidable = (solid.Collidable = (SceneAs<Level>().CoreMode == Session.CoreModes.Cold));
            }
            
            bool collidable = Collidable;
            if (collidable)
            {
                this.idleSfx.Play("event:/env/local/09_core/lavagate_idle", null, 0f);
            }
        }

        private void OnChangeMode(Session.CoreModes mode)
        {
            if (!isIce)
            {
                Collidable = (solid.Collidable = (SceneAs<Level>().CoreMode == Session.CoreModes.Hot));
            }
            else
            {
                Collidable = (solid.Collidable = (SceneAs<Level>().CoreMode == Session.CoreModes.Cold));
            }
            bool flag = !Collidable;
            if (flag)
            {
                Level level = SceneAs<Level>();
                Vector2 center = Center;
                int num = 0;
                while (num < Width)
                {
                    int num2 = 0;
                    while (num2 < Height)
                    {
                        Vector2 vector = Position + new Vector2((num + 2), (num2 + 2)) + Calc.Random.Range(-Vector2.One * 2f, Vector2.One * 2f);
                        level.Particles.Emit(FireBarrier.P_Deactivate, vector, (vector - center).Angle());
                        num2 += 4;
                    }
                    num += 4;
                }
                idleSfx.Stop(true);
            }
            else
            {
                idleSfx.Play("event:/env/local/09_core/lavagate_idle", null, 0f);
            }
        }

        private void OnPlayer(Player player)
        {
            player.Die((player.Center - Center).SafeNormalize(), false, true);
        }

        public override void Update()
        {
            bool transitioning = (base.Scene as Level).Transitioning;
            if (transitioning)
            {
                bool flag = this.idleSfx != null;
                if (flag)
                {
                    this.idleSfx.UpdateSfxPosition();
                }
            }
            else
            {
                base.Update();
            }
        }

        public override void Render()
        {
            bool collidable = this.Collidable;
            if (collidable)
            {
                base.Render();
            }
        }

        public static ParticleType P_Deactivate;

        private LavaRect Lava;

        private Solid solid;

        private SoundSource idleSfx;
    }
}
