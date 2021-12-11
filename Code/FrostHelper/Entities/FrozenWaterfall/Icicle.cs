using Celeste.Mod.Entities;
using FMOD.Studio;

namespace FrostHelper {
    /// <summary>
    /// Icicles from Frozen Waterfall
    /// </summary>
    [CustomEntity("FrostHelper/Icicle")]
    public class Icicle : Entity {
        private Sprite sprite;
        private bool moving;
        private float speed;
        private string sfx;

        public Icicle(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Add(sprite = new Sprite(GFX.Game, data.Attr("directory", "objects/FrostHelper/icicle/")));
            sprite.AddLoop("idle", "idle", 0.1f);
            sprite.Play("idle", false, false);
            sprite.CenterOrigin();
            Add(new MirrorReflection());
            Collider = new Hitbox(8f, 16f, 0f, 0f);
            Collider.CenterOrigin();
            Add(new PlayerCollider(OnPlayer));

            speed = data.Float("speed", 15f);
            sfx = data.Attr("breakSfx", SFX.game_09_iceball_break);
        }

        private void OnPlayer(Player player) {
            player.Die(Vector2.Zero);
        }

        public override void Update() {
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player != null) {
                if (player.Position.X <= Position.X + 5 && player.Position.X >= Position.X - 5 && player.Position.Y > Position.Y && player.Position.Y < Position.Y + 200) {
                    moving = true;
                }
            }

            if (moving)
                MoveVCheck(speed * Engine.DeltaTime);
            base.Update();
        }

        // tbh I have no clue what's going on in here at this point
        private bool MoveVCheck(float amount) {
            Level level = Scene as Level;
            if (!(amount < 0f && Top <= level.Bounds.Top)) {
                if (!(amount > 0f && Bottom >= level.Bounds.Bottom + 32)) {
                    for (int i = 1; i <= 4; i++) {
                        for (int j = 1; j >= -1; j -= 2) {
                            Vector2 value = new Vector2(i * j, Math.Sign(amount));
                            if (!CollideCheck<Solid>(Position + value)) {
                                MoveVExact(amount);
                                speed += 0.003f / Engine.DeltaTime;
                            } else {
                                EventInstance audio = Audio.Play(sfx, "", 0.25f);
                                audio.setVolume(0.25f);
                                level.ParticlesFG.Emit(FinalBoss.P_Burst, 1, Center, Vector2.One * 4f, Color.WhiteSmoke);
                                RemoveSelf();
                            }
                        }
                    }
                }
            }
            return true;
        }

        private void MoveVExact(float move) {
            Y += move;
        }
    }
}
