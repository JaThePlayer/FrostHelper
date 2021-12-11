namespace FrostHelper {
    [Celeste.Mod.Entities.CustomEntity("FrostHelper/TemporaryKey")]
    public class TemporaryKey : Key {
        public new bool Turning { get; private set; }

        public TemporaryKey(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset, id, data.NodesOffset(offset)) {
            this.follower = Get<Follower>();
            // Create sprite
            DynData<Key> dyndata = new DynData<Key>(this);
            sprite = dyndata.Get<Sprite>("sprite");
            Remove(sprite);
            sprite = new Sprite(GFX.Game, data.Attr("directory", "collectables/FrostHelper/keytemp") + "/");
            sprite.Justify = new Vector2(0.5f, 0.5f);
            sprite.AddLoop("idle", "idle", 0.1f);
            sprite.AddLoop("enter", "enter", 0.1f);
            sprite.Play("idle");
            Add(sprite);
            dyndata.Set("sprite", sprite);
            Follower follower = this.follower;
            follower.OnLoseLeader = (Action) Delegate.Combine(follower.OnLoseLeader, new Action(Dissolve));
            this.follower.PersistentFollow = false; // was false
            Add(new TransitionListener {
                OnOut = delegate (float f) {
                    StartedUsing = false;
                    if (!IsUsed) {
                        Dissolve();
                    }
                }
            });
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (scene is Level level) {
                start = Position;
                startLevel = level.Session.Level;
            }
        }

        public override void Update() {
            Level level = Scene as Level;
            Session session = level?.Session;
            if (IsUsed && !wasUsed) {
                wasUsed = true;
            }
            if (!dissolved && !IsUsed && !base.Turning && session != null && session.Keys.Contains(ID)) {
                session.DoNotLoad.Remove(ID);
                session.Keys.Remove(ID);
                session.UpdateLevelStartDashes();
            }
            base.Update();
        }

        public void Dissolve() {
            bool flag = dissolved || IsUsed || base.Turning;
            if (!flag) {
                dissolved = true;
                bool flag2 = follower.Leader != null;
                if (flag2) {
                    Player player = follower.Leader.Entity as Player;
                    player.StrawberryCollectResetTimer = 2.5f;
                    follower.Leader.LoseFollower(follower);
                }
                Add(new Coroutine(DissolveRoutine(), true));
            }
        }

        private IEnumerator DissolveRoutine() {
            Level level = Scene as Level;
            Session session = level.Session;
            if (session.DoNotLoad.Contains(ID))
                session.DoNotLoad.Remove(ID);
            if (session.Keys.Contains(ID))
                session.Keys.Remove(ID);
            session.UpdateLevelStartDashes();
            Audio.Play("event:/game/general/seed_poof", Position);
            Collidable = false;
            sprite.Scale = Vector2.One * 0.5f;
            yield return 0.05f;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            int num;
            for (int i = 0; i < 6; i = num + 1) {
                float dir = Calc.Random.NextFloat(6.28318548f);
                level.ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
                num = i;
            }
            sprite.Scale = Vector2.Zero;
            Visible = false;
            bool flag = level.Session.Level != startLevel;
            if (flag) {
                RemoveSelf();
                yield break;
            }
            yield return 0.3f;
            dissolved = false;
            Audio.Play("event:/game/general/seed_reappear", Position);
            Position = start;
            sprite.Scale = Vector2.One;
            Visible = true;
            Collidable = true;
            level.Displacement.AddBurst(Position, 0.2f, 8f, 28f, 0.2f, null, null);
            yield break;
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Session session = (scene as Level).Session;
            if (session.DoNotLoad.Contains(ID))
                session.DoNotLoad.Remove(ID);
            if (session.Keys.Contains(ID))
                session.Keys.Remove(ID);
        }

        private Sprite sprite;

        private Follower follower;

        private Vector2 start;

        private string startLevel;

        private bool dissolved;

        private bool wasUsed;
    }
}

