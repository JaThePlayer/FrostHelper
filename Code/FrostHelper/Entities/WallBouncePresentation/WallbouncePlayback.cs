namespace FrostHelper.Entities.WallBouncePresentation {
    public class WallbouncePlayback {
        public PlayerPlayback Playback { get; private set; }

        public WallbouncePlayback(string name, Vector2 offset) {
            List<Player.ChaserState> timeline = PlaybackData.Tutorials[name];
            Playback = new PlayerPlayback(offset, PlayerSpriteMode.Madeline, timeline);
            tag = Calc.Random.Next();


        }

        public void Update() {
            Playback.Update();
            Playback.Hair.AfterUpdate();
            if (Playback.Sprite.CurrentAnimationID == "dash" && Playback.Sprite.CurrentAnimationFrame == 0) {
                if (!dashing) {
                    dashing = true;
                    Celeste.Celeste.Freeze(0.05f);
                    SlashFx.Burst(Playback.Center, (-Vector2.UnitY).Angle()).Tag = tag;
                    dashTrailTimer = 0.1f;
                    dashTrailCounter = 2;
                    CreateTrail();

                    launchedDelay = 0.15f;
                }
            } else {
                dashing = false;
            }
            if (dashTrailTimer > 0f) {
                dashTrailTimer -= Engine.DeltaTime;
                if (dashTrailTimer <= 0f) {
                    CreateTrail();
                    dashTrailCounter--;
                    if (dashTrailCounter > 0) {
                        dashTrailTimer = 0.1f;
                    }
                }
            }
            if (launchedDelay > 0f) {
                launchedDelay -= Engine.DeltaTime;
                if (launchedDelay <= 0f) {
                    launched = true;
                    launchedTimer = 0f;
                }
            }
            if (launched) {
                float prevVal = launchedTimer;
                launchedTimer += Engine.DeltaTime;
                if (launchedTimer >= 0.5f) {
                    launched = false;
                    launchedTimer = 0f;
                } else if (Calc.OnInterval(launchedTimer, prevVal, 0.15f)) {
                    SpeedRing speedRing = Engine.Pooler.Create<SpeedRing>().Init(Playback.Center, (Playback.Position - Playback.LastPosition).Angle(), Color.White);
                    speedRing.Tag = tag;
                    Engine.Scene.Add(speedRing);
                }
            }
        }

        public void Render(Vector2 position) {
            Matrix transformationMatrix = Matrix.CreateScale(4f) * Matrix.CreateTranslation(position.X, position.Y, 0f);
            Draw.SpriteBatch.End();
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, transformationMatrix);
            foreach (Entity entity in Engine.Scene.Tracker.GetEntities<TrailManager.Snapshot>()) {
                if (entity.Tag == tag) {
                    entity.Render();
                }
            }
            foreach (Entity entity2 in Engine.Scene.Tracker.GetEntities<SlashFx>()) {
                if (entity2.Tag == tag && entity2.Visible) {
                    entity2.Render();
                }
            }
            foreach (Entity entity3 in Engine.Scene.Tracker.GetEntities<SpeedRing>()) {
                if (entity3.Tag == tag) {
                    entity3.Render();
                }
            }
            if (Playback.Visible) {
                Playback.Render();
            }
            OnRender?.Invoke();
            Draw.SpriteBatch.End();
            Draw.SpriteBatch.Begin();
        }

        private void CreateTrail() {
            TrailManager.Add(Playback.Position, Playback.Sprite, Playback.Hair, Playback.Sprite.Scale, Player.UsedHairColor, 0, 1f, false, false).Tag = tag;
        }

        public Action OnRender;

        private float dashTrailTimer;

        private int dashTrailCounter;

        private bool dashing;

        private bool launched;

        private float launchedDelay;

        private float launchedTimer;

        private int tag;
    }
}
