using Celeste.Mod.Entities;

namespace FrostHelper {
    [CustomEntity("FrostHelper/Voidstone")]
    [Tracked]
    public class Voidstone : Solid {
        public static ParticleType BoostParticle => _boostParticle is null ? (_boostParticle = new ParticleType() {
            Source = GFX.Game["particles/shard"],
            Color = FillColor,
            Color2 = FillColor * 0.7f,
            ColorMode = ParticleType.ColorModes.Fade,
            FadeMode = ParticleType.FadeModes.Late,
            RotationMode = ParticleType.RotationModes.Random,
            Size = 0.8f,
            SizeRange = 0.4f,
            SpeedMin = 20f,
            SpeedMax = 40f,
            SpeedMultiplier = 0.2f,
            LifeMin = 0.4f,
            LifeMax = 0.6f,
            DirectionRange = 6.28318548f
        }) : _boostParticle;
        private static ParticleType _boostParticle;

        [OnLoad]
        public static void Load() {
            On.Celeste.Player.SuperWallJump += Player_SuperWallJump;
        }

        [OnUnload]
        public static void Unload() {
            On.Celeste.Player.SuperWallJump -= Player_SuperWallJump;
        }

        public override void Render() {
            //return;

            Camera c = SceneAs<Level>().Camera;
            Draw.Rect(Collider, Color.Black);
            VirtualRenderTarget tempA = GameplayBuffers.TempA;
            GameplayRenderer.End();
            Engine.Instance.GraphicsDevice.SetRenderTarget(tempA);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            Engine.Instance.GraphicsDevice.Clear(Color.White);
            Draw.Rect(0, 0, GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height, Color.DarkSlateGray);

            GameplayRenderer.End();

            Effect eff = ModIntegration.ShaderHelperIntegration.GetEffect("trippy");
            eff.Parameters["Time"].SetValue(Scene.TimeActive);

            Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
            int s = GameplayBuffers.Gameplay.Width / 320;
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, eff, c.Matrix * s);


            Draw.SpriteBatch.Draw(tempA, (Position - c.Position) * s + c.Position, new Rectangle((int) Position.X * s, (int) Position.Y * s, (int) Width * s, (int) Height * s), Color.Black);

            GameplayRenderer.End();
            GameplayRenderer.Begin();
        }

        // Check if the player is next to a voidstone when wallbouncing
        private static void Player_SuperWallJump(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir) {
            orig(self, dir);

            Voidstone stone;
            if ((stone = self.CollideFirst<Voidstone>(self.Position - Vector2.UnitX * dir * 5f)) != null) {
                stone.Used(self);
            }
        }

        public static Color FillColor = Calc.HexToColor("282a2e");

        public static void CreateTrail(Player player) {
            Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float) player.Facing, player.Sprite.Scale.Y);

            TrailManager.Add(player.Position - Vector2.UnitY, player.Get<PlayerSprite>(), player.Get<PlayerHair>(), scale, FillColor, player.Depth + 1, 1f);

        }

        public Player PlayerThatWallbounced;

        public Voidstone(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, true) {
            Add(new DashListener(OnDash));
            Add(new ClimbBlocker(false));
            Depth = Depths.Top;
        }

        public void OnDash(Vector2 dir) {
            PlayerThatWallbounced = null;
        }

        public override void Update() {
            base.Update();
            if (PlayerThatWallbounced != null) {
                if (PlayerThatWallbounced.Speed.Length() > 300f) {
                    if (Scene.OnInterval(0.1f))
                        CreateTrail(PlayerThatWallbounced);
                    SceneAs<Level>().ParticlesBG.Emit(BoostParticle, PlayerThatWallbounced.Position);
                } else {
                    PlayerThatWallbounced = null;
                }

            }
        }

        public void Used(Player player) {
            player.Speed *= 2f;
            PlayerThatWallbounced = player;
        }
    }
}
